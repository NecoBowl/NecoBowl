using NecoBowl.Core.Tags;
using NLog;

namespace NecoBowl.Core.Sport.Play;

internal interface IMutationReceiver
{
    public void BufferMutation(Mutation mutation);
}

public record SubstepContents(
    IReadOnlyCollection<Mutation.BaseMutation> Mutations,
    IReadOnlyCollection<NecoUnitMovement> Movements);

internal class NecoPlayStepperNew : IMutationReceiver
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly Playfield Field;

    private readonly List<Mutation> MutationLog = new();

    private readonly Dictionary<NecoUnitId, NecoUnitMovement> PendingMovements = new();
    private readonly List<Mutation.BaseMutation> PendingMutations = new();
    private int LastTurnMutationLogEndIndex;

    public NecoPlayStepperNew(Playfield field)
    {
        Field = field;
    }

    private bool MutationsRemaining
        => PendingMovements.Values.Any() || PendingMutations.Any();

    public void BufferMutation(Mutation mutation)
    {
        switch (mutation) {
            case Mutation.BaseMutation baseMutation:
                PendingMutations.Add(baseMutation);
                break;
            default:
                throw new ArgumentException($"unknown type: {mutation.GetType()}");
        }
    }

    /// <summary>Run the step, modifying this stepper's <see cref="Playfield" />.</summary>
    /// <returns>The log of mutations that occurred during the step.</returns>
    public IEnumerable<SubstepContents> Process()
    {
        var substeps = new List<SubstepContents>();

        PopUnitActions(out var chainedActions);

        AddPreStepMutations();

        // Begin the substep loop
        while (MutationsRemaining) {
            List<Mutation.BaseMutation> outputMutations = new();
            List<NecoUnitMovement> outputMovements = new();

            // Perform early mutations.
            // For example, a unit getting pushed adds its movement here.
            foreach (var mutation in PendingMutations) {
                mutation.EarlyMutate(Field, new(PendingMovements, PendingMutations));
            }

            // Perform regular mutation, since they might affect movement.
            // This will consume all mutations, and enqueue mutations and movements created as a result.
            outputMutations.AddRange(PendingMutations);
            ProcessMutations();

            // TempZone needs to be cleared when moving.
            Field.TempUnitZone.Clear();

            // Perform movement.
            var mover = new UnitMover(this, Field, PendingMovements.Values);
            mover.MoveUnits(out var movements);
            outputMovements.AddRange(movements);
            PendingMovements.Clear();

            // Add movements/mutations from chain actions
            foreach (var (id, action) in chainedActions.Where(kv => kv.Value is not null)) {
                EnqueueMutationFromAction(id, action!.Result(id, Field.AsReadOnly()));
                chainedActions[id] = action.Next;
            }

            substeps.Add(new(outputMutations, outputMovements));
        }

        return substeps;
    }

    private void PopUnitActions(out Dictionary<NecoUnitId, NecoUnitAction?> chainedActions)
    {
        chainedActions = new();
        // Perform the actions of each unit to populate the lists
        foreach (var (pos, unit) in Field.GetAllUnits()) {
            var action = unit.PopAction();
            var result = action.Result(unit.Id, Field.AsReadOnly());
            EnqueueMutationFromAction(unit.Id, result);

            chainedActions[unit.Id] = action.Next;
        }
    }

    private void AddPreStepMutations()
    {
        // Case: Unit with Pusher
        // TAGIMPL:Pusher
        // TODO Modularize
        foreach (var (pos, unit) in Field.GetAllUnits().Where(unit => unit.Item2.Tags.Contains(NecoUnitTag.Pusher))) {
            var movement = PendingMovements[unit.Id];
            if (movement.IsChange) {
                if (Field.TryGetUnit(movement.NewPos, out var targetUnit)) {
                    PendingMutations.Add(
                        new Mutation.UnitPushes(unit.Id, targetUnit!.Id, movement.AsDirection()));
                }
            }
        }
    }

    /// <summary>
    /// Performs the effects of each mutation in <see cref="PendingMutations" />, removing the mutation in the process.
    /// Populates it with the mutations that result from running the current ones. Also adds the ran mutations to the
    /// <see cref="MutationLog" />.
    /// </summary>
    private void ProcessMutations()
    {
        var baseMutations = PendingMutations.ToList();

        var substepContext = new NecoSubstepContext(PendingMovements, baseMutations);

        // RUN MUTATION FUNCTIONS

        // First run the Prepare() and remove the mutation if it wants to cancel
        for (var i = baseMutations.Count - 1; i >= 0; i--) {
            // i can't believe i have to use this stupid loop
            if (baseMutations[i].Prepare(substepContext, Field.AsReadOnly())) {
                baseMutations.RemoveAt(i);
            }
        }

        // Then run the mutate passes
        foreach (var func in Mutation.ExecutionOrder) {
            foreach (var mutation in baseMutations) {
                func.Invoke(mutation, substepContext, Field);
            }
        }

        var tempMutations = new List<Mutation.BaseMutation>();

        foreach (var (pos, unit) in Field.GetAllUnits()) {
            foreach (var mutation in baseMutations) {
                var reaction = unit.Reactions.SingleOrDefault(r => r.MutationType == mutation.GetType());
                if (reaction is not null) {
                    tempMutations.AddRange(reaction.Reaction(unit, Field.AsReadOnly(), mutation));
                }
            }
        }

        // Prepare the mutations for next run
        foreach (var mutation in baseMutations) {
            tempMutations.AddRange(mutation.GetResultantMutations(Field.AsReadOnly()));
        }

        PendingMutations.Clear();

        foreach (var mutation in tempMutations) {
            PendingMutations.Add(mutation);
            break;
        }
    }

    private void EnqueueMutationFromAction(NecoUnitId uid, NecoUnitActionResult result)
    {
        switch (result) {
            // Cases where an error ocurred 
            case { ResultKind: NecoUnitActionResult.Kind.Error }: {
                var unit = Field.GetUnit(uid);
                Logger.Error($"Error ocurred while processing action for {unit}:");
                Logger.Error($"{result.Exception}\n{result.Exception!.StackTrace}");
                break;
            }

            // Cases where we reset movement
            case {
                ResultKind: NecoUnitActionResult.Kind.Failure,
                StateChange: NecoUnitActionOutcome.UnitTranslated translation
            }: {
                var unit = Field.GetUnit(uid, out var pos);
                PendingMovements[uid] = translation.Movement;
                break;
            }

            // Cases where things happen
            case { StateChange: NecoUnitActionOutcome.UnitTranslated translation }: {
                PendingMovements[uid] = translation.Movement;
                break;
            }

            case { StateChange: NecoUnitActionOutcome.UnitChanged unitChanged }: {
                PendingMutations.Add(new Mutation.UnitGetsMod(uid, unitChanged.Mod));
                break;
            }

            case { StateChange: NecoUnitActionOutcome.ThrewItem threwItem }: {
                PendingMutations.Add(
                    new Mutation.UnitThrowsItem(uid, threwItem.Item.Id, threwItem.Destination));
                break;
            }

            case { StateChange: NecoUnitActionOutcome.NothingHappened }: {
                break;
            }

            case { ResultKind: NecoUnitActionResult.Kind.Failure }: {
                Logger.Debug($"Action failed: {result.Message}");
                break;
            }

            default: {
                throw new ArgumentException($"Unhandled outcome type {result.StateChange}");
            }
        }
    }
}

static file class Extension
{
    public static void RemoveUnitPair(this Dictionary<NecoUnitId, NecoUnitMovement> source, UnitMovementPair unitPair)
    {
        source.Remove(unitPair.Movement1.UnitId);
        source.Remove(unitPair.Movement2.UnitId);
    }
}
