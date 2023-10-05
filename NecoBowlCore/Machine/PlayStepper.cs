using NecoBowl.Core.Sport.Play;
using NecoBowl.Core.Tags;
using NLog;

namespace NecoBowl.Core.Machine;

internal interface IMutationReceiver
{
    public void BufferMutation(Mutation mutation);
}

internal record SubstepContents(
    IReadOnlyCollection<Mutation> Mutations,
    IReadOnlyCollection<TransientUnit> Movements);

internal class PlayStepper : IMutationReceiver
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly Playfield Field;

    private readonly List<Mutation> MutationLog = new();

    private readonly Dictionary<NecoUnitId, TransientUnit> PendingMovements = new();
    private readonly List<Mutation> PendingMutations = new();

    public PlayStepper(Playfield field)
    {
        Field = field;
    }

    private bool MutationsRemaining
        => PendingMovements.Values.Any() || PendingMutations.Any();

    public void BufferMutation(Mutation mutation)
    {
        PendingMutations.Add(mutation);
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
            List<Mutation> outputMutations = new();
            List<TransientUnit> outputMovements = new();

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

    private void PopUnitActions(out Dictionary<NecoUnitId, Behavior?> chainedActions)
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
                    PendingMutations.Add(new UnitPushes(unit.Id, targetUnit!.Id, movement.AsDirection()));
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

        var tempMutations = new List<Mutation>();

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

    private void EnqueueMutationFromAction(NecoUnitId uid, BehaviorOutcome result)
    {
        switch (result) {
            // Cases where an error ocurred 
            case { ResultKind: BehaviorOutcome.Kind.Error }: {
                var unit = Field.GetUnit(uid);
                Logger.Error($"Error ocurred while processing action for {unit}:");
                Logger.Error($"{result.Exception}\n{result.Exception!.StackTrace}");
                break;
            }

            // Cases where things happen
            case BehaviorOutcome.Translate translation: {
                PendingMovements[uid] = translation.Movement;
                break;
            }

            // Note that we catch movements before checking for failure.
            case { ResultKind: BehaviorOutcome.Kind.Failure }: {
                Logger.Debug($"Action failed: {result.Message}");
                break;
            }

            case BehaviorOutcome.Mutate mutation: {
                PendingMutations.Add(mutation.Mutation);
                break;
            }

            default: {
                throw new ArgumentException($"Unhandled outcome type {result.GetType()}");
            }
        }
    }
}

static file class Extension
{
    public static void RemoveUnitPair(this Dictionary<NecoUnitId, TransientUnit> source, UnitMovementPair unitPair)
    {
        source.Remove(unitPair.Movement1.UnitId);
        source.Remove(unitPair.Movement2.UnitId);
    }
}
