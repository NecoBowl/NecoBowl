using AnyOfTypes;
using NecoBowl.Core.Sport.Play;
using NecoBowl.Core.Tags;
using NLog;

namespace NecoBowl.Core.Machine;

internal interface IPlayfieldChangeReceiver
{
    public void BufferMovement(TransientUnit movement);
    public void BufferMutation(BaseMutation mutation);

    public sealed void EnqueuePlayfieldChange(AnyOf<BaseMutation, TransientUnit> change)
    {
        if (change.IsFirst) {
            BufferMutation(change.First);
        }
        else if (change.IsSecond) {
            BufferMovement(change.Second);
        }
    }

    public BaseMutation? MutationIsBuffered<T>(NecoUnitId subject)
        where T : BaseMutation;
}

internal record SubstepContents(
    IReadOnlyCollection<BaseMutation> Mutations,
    IReadOnlyCollection<TransientUnit> Movements);

internal class PlayStepper : IPlayfieldChangeReceiver
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly Playfield Field;

    private readonly List<BaseMutation> MutationLog = new();
    private readonly List<BaseMutation> OldMutationBuffer = new();

    private readonly Dictionary<NecoUnitId, TransientUnit> PendingMovements = new();
    private readonly List<BaseMutation> PendingMutations = new();

    public PlayStepper(Playfield field)
    {
        Field = field;
    }

    private bool MutationsRemaining
        => PendingMovements.Values.Any() || PendingMutations.Any();

    public void BufferMovement(TransientUnit movement)
    {
        Logger.Trace($"{nameof(BufferMovement)} <- {movement}");
        PendingMovements[movement.UnitId] = movement;
    }

    public void BufferMutation(BaseMutation mutation)
    {
        PendingMutations.Add(mutation);
    }

    public BaseMutation? MutationIsBuffered<T>(NecoUnitId subject) where T : BaseMutation
    {
        return OldMutationBuffer.FirstOrDefault(mut => mut is T && mut.Subject == subject);
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
            List<BaseMutation> outputMutations = new();
            List<TransientUnit> outputMovements = new();

            // Perform early mutations.
            // For example, a unit getting pushed adds its movement here.
            foreach (var mutation in PendingMutations) {
                mutation.EarlyMutate(Field, this);
            }

            // Perform regular mutation, since they might affect movement.
            // This will consume all mutations, and enqueue mutations and movements created as a result.
            OldMutationBuffer.AddRange(PendingMutations.ToList());
            PendingMutations.Clear();
            outputMutations.AddRange(OldMutationBuffer);
            new FieldMutator(this, Field, OldMutationBuffer).MutateField();
            OldMutationBuffer.Clear();

            // Perform movement.
            var mover = new UnitMover(this, Field, PendingMovements.Values);
            mover.MoveUnits(out var movements);
            outputMovements.AddRange(movements);
            PendingMovements.Clear();

            // Add movements/mutations from chain actions
            foreach (var (id, action) in chainedActions.Where(kv => kv.Value is { })) {
                EnqueueMutationFromAction(id, action!.Result(id, Field.AsReadOnly()));
                chainedActions[id] = action.Next;
            }

            substeps.Add(new(outputMutations, outputMovements));
        }

        return substeps;
    }

    private void PopUnitActions(out Dictionary<NecoUnitId, BaseBehavior?> chainedActions)
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

            case BehaviorOutcome.Nothing nothing: {
                break;
            }

            default: {
                throw new ArgumentException($"Unhandled outcome type {result.GetType()}");
            }
        }
    }
}

internal static class Extension
{
    public static void RemoveUnitPair(this Dictionary<NecoUnitId, TransientUnit> source, UnitMovementPair unitPair)
    {
        source.Remove(unitPair.Movement1.UnitId);
        source.Remove(unitPair.Movement2.UnitId);
    }
}
