using neco_soft.NecoBowlCore.Tags;
using NLog;

namespace neco_soft.NecoBowlCore.Action;

internal class NecoPlayStepperNew
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly NecoField Field;
    private readonly List<NecoPlayfieldMutation> MutationHistory = new();

    private readonly Dictionary<NecoUnitId, NecoPlayfieldMutation.MovementMutation> PendingMovements = new();
    private readonly List<NecoPlayfieldMutation.BaseMutation> PendingMutations = new();

    public NecoPlayStepperNew(NecoField field)
    {
        Field = field;
    }

    private bool MutationsRemaining
        => PendingMovements.Values.Any() || PendingMutations.Any();

    /// <summary>Run the step, modifying this stepper's <see cref="NecoField" />.</summary>
    /// <returns>The log of mutations that occurred during the step.</returns>
    public IEnumerable<NecoPlayfieldMutation> Process()
    {
        List<NecoPlayfieldMutation> stepMutations = new();

        PopUnitActions(out var chainedActions);

        AddPreStepMutations();

        // Begin the substep loop
        while (MutationsRemaining) {
            // Perform early mutations.
            // For example, a unit getting pushed adds its movement here.
            foreach (var mutation in PendingMutations) {
                mutation.EarlyMutate(Field, new(PendingMovements, PendingMutations));
            }

            // Perform regular mutation, since they might affect movement.
            ConsumeMutations();
            stepMutations.AddRange(PendingMutations);

            Field.TempUnitZone.Clear();

            // Perform movement
            var mover = new NecoPlayfieldUnitMover(Field, PendingMovements.Select(kv => kv.Value.Movement));
            mover.MoveUnits(out var movementMutations);
            stepMutations.AddRange(movementMutations);
            PendingMutations.AddRange(
                movementMutations.Where(m => m is NecoPlayfieldMutation.BaseMutation)
                    .Cast<NecoPlayfieldMutation.BaseMutation>());

            PendingMovements.Clear();

            // Add movements/mutations from multi-actions
            foreach (var (id, action) in chainedActions.Where(kv => kv.Value is not null)) {
                EnqueueMutationFromAction(id, action!.Result(id, Field.AsReadOnly()));
                chainedActions[id] = action.Next;
            }
        }

        foreach (var mut in stepMutations) {
            Logger.Debug(mut);
        }

        MutationHistory.AddRange(stepMutations);

        return stepMutations;
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
            var movement = PendingMovements[unit.Id].Movement;
            if (movement.IsChange) {
                if (Field.TryGetUnit(movement.NewPos, out var targetUnit)) {
                    PendingMutations.Add(
                        new NecoPlayfieldMutation.UnitPushes(unit.Id, targetUnit!.Id, movement.AsDirection()));
                }
            }
        }
    }

    /// <summary>
    /// Performs the effects of each mutation in <see cref="PendingMutations" />, removing the mutation in the process.
    /// Populates it with the mutations that result from running the current ones.
    /// </summary>
    private void ConsumeMutations()
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
        foreach (var func in NecoPlayfieldMutation.ExecutionOrder) {
            foreach (var mutation in baseMutations) {
                func.Invoke(mutation, substepContext, Field);
            }
        }

        foreach (var mutation in baseMutations) {
            MutationHistory.Add(mutation);
        }

        var tempMutations = new List<NecoPlayfieldMutation>();

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
            switch (mutation) {
                case NecoPlayfieldMutation.MovementMutation moveMut: {
                    PendingMovements[moveMut.Subject] = moveMut;
                    break;
                }
                case NecoPlayfieldMutation.BaseMutation baseMut: {
                    PendingMutations.Add(baseMut);
                    break;
                }
            }
        }
    }

    private void EnqueueMutationFromAction(NecoUnitId uid, NecoUnitActionResult result)
    {
        NecoPlayfieldMutation.MovementMutation Default(NecoUnit unit, Vector2i pos)
        {
            return new(new(unit, pos, pos));
        }

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
                PendingMovements[uid] = new(new(translation.Movement, pos, pos, translation.Movement));
                break;
            }

            // Cases where things happen
            case { StateChange: NecoUnitActionOutcome.UnitTranslated translation }: {
                PendingMovements[uid] = new(new(translation.Movement));
                break;
            }

            case { StateChange: NecoUnitActionOutcome.UnitChanged unitChanged }: {
                PendingMutations.Add(new NecoPlayfieldMutation.UnitGetsMod(uid, unitChanged.Mod));
                break;
            }

            default: {
                var unit = Field.GetUnit(uid, out var pos);
                PendingMovements[uid] = Default(unit, pos);
                break;
            }
        }
    }
}

internal static class Extension
{
    public static void RemoveUnitPair(this Dictionary<NecoUnitId, NecoUnitMovement> source, UnitMovementPair unitPair)
    {
        source.Remove(unitPair.Movement1.UnitId);
        source.Remove(unitPair.Movement2.UnitId);
    }
}
