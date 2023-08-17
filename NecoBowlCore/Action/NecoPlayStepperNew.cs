using System.Diagnostics.CodeAnalysis;

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
    private readonly List<NecoPlayfieldMutation.BaseMutation> PendingMutationsPostProcessing = new();

    public NecoPlayStepperNew(NecoField field)
    {
        Field = field;
    }

    private bool MutationsRemaining
        => PendingMovements.Values.Any(m => m.Movement.IsChange || m.Movement.IsChangeInSource)
         || PendingMutations.Any();

    [SuppressMessage("ReSharper", "RedundantBoolCompare")]
    public IEnumerable<NecoPlayfieldMutation> Process()
    {
        List<NecoPlayfieldMutation> stepMutations = new();

        // Perform the actions of each unit to populate the lists
        foreach (var (pos, unit) in Field.GetAllUnits()) {
            var result = unit.PopAction().Result(unit.Id, Field.AsReadOnly());
            SetMovementFromAction(unit.Id, result);
        }

        AddPreMovementMutations();

        // Begin the substep loop
        while (MutationsRemaining) {
            ProcessPreMovementMutations();

            // TODO Order the mutations before processing them.
            // First, process mutations that might effect how movement happens.
            ConsumeMutations(out var stepStartMutations);
            stepMutations.AddRange(stepStartMutations);

            FixFailedActionResultMovements();
            ResolvePendingMovementCollisions();

            if (PendingMovements.Any()) {
                ConsumeMovements(out var resultantMutations);
                stepMutations.AddRange(resultantMutations);
                PendingMovements.Clear();
            }

            // Add the post-processing now so the next substep can see it
            PendingMutationsPostProcessing.ForEach(PendingMutations.Add);
            PendingMutationsPostProcessing.Clear();
        }

        foreach (var entry in stepMutations) {
            Logger.Debug(entry);
        }

        MutationHistory.AddRange(stepMutations);

        return stepMutations;
    }

    private void ProcessPreMovementMutations()
    {
        foreach (var mutation in PendingMutations) {
            mutation.PreMovementMutate(Field, new(PendingMovements));
        }
    }

    private void ConsumeMovements(out IEnumerable<NecoPlayfieldMutation> resultantMutations)
    {
        var _resultantMutations = new List<NecoPlayfieldMutation>();

        // Split the movements into two groups; one that has no movement, and the other that has attempted/successful movements.
        var finalMovements = PendingMovements.Values.GroupBy(m => m.Movement.IsChangeInSource).ToList();
        var unitBuffer = new Dictionary<NecoUnitId, NecoUnitMovement>();
        foreach (var moveMut in finalMovements.Single(g => g.Key)) {
            // For any unit that is going to move, place them in the movement buffer.
            var unit = Field[moveMut.OldPos].Unit!;
            var movementMutation = PendingMovements[moveMut.Subject];
            var movement = movementMutation.Movement;
            unitBuffer[unit.Id] = movement;
            Field[movement.OldPos] = Field[movement.OldPos] with {
                Unit = null
            };
            PendingMovements.Remove(movementMutation.Subject);
        }

        // Perform each movement.
        foreach (var (uid, movement) in unitBuffer.Select(kv => (kv.Key, kv.Value))) {
            // Sanity check for collisions that didn't get handled
            var collisionError = unitBuffer.Values.Where(m => m.NewPos == movement.NewPos && m != movement);
            if (collisionError.Any() || Field[movement.NewPos].Unit is not null) {
                throw new NecoPlayfieldMutationException("collision in the unit buffer");
            }

            // Update the unit position
            Field[movement.NewPos] = Field[movement.NewPos] with {
                Unit = movement.Unit
            };

            if (movement.IsChange) {
                _resultantMutations.Add(new NecoPlayfieldMutation.MovementMutation(movement));
            }
            else if (movement.Source?.ResultKind == NecoUnitActionResult.Kind.Failure || !movement.IsChange) {
                if (movement.Source!.StateChange is NecoUnitActionOutcome.UnitTranslated attemptedTranslation) {
                    // Add bump event
                    if (!PendingMutations.Any(m => m.Subject == uid)) {
                        _resultantMutations.Add(new NecoPlayfieldMutation.UnitBumps(uid,
                            attemptedTranslation.Movement.AsDirection()));
                    }
                }
                else {
                    Logger.Warn($"unknown failure for movement: {movement.Source.StateChange}");
                }
            }
        }

        resultantMutations = _resultantMutations;
    }

    private void AddPreMovementMutations()
    {
        // Case: Unit with Pusher
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

    private void ResolvePendingMovementCollisions()
    {
        var movementsToReset = new List<NecoPlayfieldMutation.MovementMutation>();
        var spaceSwapMovements = new List<NecoPlayfieldMutation.MovementMutation>();

        do {
            // Then resolve movements, which may create more mutations.
            foreach (var (uid, mut) in PendingMovements) {
                // Resolve space swaps first as they are a special case.
                if (UpdatePendingMovementsBySpaceSwap(mut.Movement)) {
                    movementsToReset.Add(mut);
                    spaceSwapMovements.Add(mut);
                }
            }

            foreach (var (uid, mut) in PendingMovements) {
                // Then figure out if this unit will get its movement canceled due to space conflicts.
                if (UpdatePendingMutationsBySpaceConflict(mut.Movement, spaceSwapMovements)) {
                    movementsToReset.Add(mut);
                }
            }

            foreach (var moveMut in movementsToReset) {
                PendingMovements[moveMut.Subject] = new(new(moveMut.Movement, moveMut.OldPos));
            }
        } while (GetConflicts().Any());
    }

    private IEnumerable<List<NecoPlayfieldMutation.MovementMutation>> GetConflicts()
    {
        return PendingMovements.GroupBy(kv => kv.Value.Movement.NewPos)
            .Where(g => g.Count() > 1)
            .Select(g => g.Select(i => i.Value).ToList());
    }

    /// <summary>
    ///     Performs the effects of each mutation in <see cref="PendingMutations" />, removing the mutation in the process.
    ///     Populates it with the mutations that result from running the current ones.
    /// </summary>
    private void ConsumeMutations(out List<NecoPlayfieldMutation> mutations)
    {
        var baseMutations = PendingMutations.ToList();

        var substepContext = new NecoSubstepContext(PendingMovements);
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

        mutations = new();
        mutations.AddRange(PendingMutations);

        // We have processed the pending mutations.
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

    private void FixFailedActionResultMovements()
    {
        // Fix the movement data for Movement entries that are moving to invalid locations.
        // The data behind their original attempt will still live on in the Source property.
        foreach (var movementMutation in PendingMovements.Values
                     .Where(m => m.Movement.Source?.ResultKind == NecoUnitActionResult.Kind.Failure)) {
            movementMutation.Movement = new(movementMutation.Movement, movementMutation.OldPos);
        }
    }

    private bool UpdatePendingMutationsBySpaceConflict(NecoUnitMovement movement,
                                                       IEnumerable<NecoPlayfieldMutation.MovementMutation>
                                                           spaceSwapMutations)
    {
        IEnumerable<NecoUnitMovement> OtherMovements()
        {
            return PendingMovements.Select(m => m.Value.Movement).Where(m => m != movement);
        }

        var shouldReset = false;

        var conflictsEnumerable = OtherMovements()
            .Where(m => !spaceSwapMutations.Any(mut => mut.Movement == m))
            .Where(m => m.NewPos == movement.NewPos);

        // Find conflicts that would occur if each unit did its movement.
        var conflicts = conflictsEnumerable.ToList();

        // Figure out what to do with this unit based on each conflict.
        foreach (var conflict in conflicts) {
            var unitPair = new UnitMovementPair(movement, conflict);

            // Prioritize combat first.
            if (unitPair.UnitsCanFight() && movement.IsChange) {
                PendingMutations.Add(new NecoPlayfieldMutation.UnitAttacks(Field.AsReadOnly(),
                    movement.UnitId,
                    conflict.UnitId));
                shouldReset = true;
            }
            else {
                // Pickup 
                if (unitPair.TryGetUnitsBy(m => m.Unit.Tags.Contains(NecoUnitTag.Item),
                        m => m.Unit.Tags.Contains(NecoUnitTag.Carrier),
                        out var itemUnit,
                        out var carrierUnit)) {
                    if (carrierUnit == movement) {
                        // Pickup occurs
                        PendingMutationsPostProcessing.Add(new NecoPlayfieldMutation.UnitPicksUpItem(
                            carrierUnit!.UnitId,
                            itemUnit!.UnitId,
                            movement));
                        shouldReset = true;
                        goto DoneCheckingUnitPair;
                    }
                }

                // Friendly unit conflict 
                if (DecideCollisionWinner(unitPair) != movement) {
                    shouldReset = true;
                }
            }

        DoneCheckingUnitPair: ;
        }

        return shouldReset;
    }

    /// <returns>True if the unit is undergoing a space swap; false otherwise.</returns>
    private bool UpdatePendingMovementsBySpaceSwap(NecoUnitMovement movement)
    {
        IEnumerable<NecoUnitMovement> OtherMovements()
        {
            return PendingMovements.Select(m => m.Value.Movement).Where(m => movement != m);
        }

        var swap = OtherMovements()
            .SingleOrDefault(m => m.NewPos == movement.OldPos && m.OldPos == movement.NewPos);

        if (swap is null) {
            return false;
        }

        var unitPair = new UnitMovementPair(movement, swap);
        if (unitPair.UnitsAreEnemies()) {
            PendingMutations.Add(
                new NecoPlayfieldMutation.UnitAttacks(Field.AsReadOnly(), movement.UnitId, swap.UnitId));
        }
        else {
            // Friendly space swappers will just bump off each other.
            PendingMutations.Add(new NecoPlayfieldMutation.UnitBumps(movement.UnitId, swap.AsDirection()));
        }

        return true;
    }

    /// <summary>
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="result"></param>
    /// <exception cref="Exception"></exception>
    private void SetMovementFromAction(NecoUnitId uid, NecoUnitActionResult result)
    {
        NecoPlayfieldMutation.MovementMutation Default(NecoUnit unit, Vector2i pos)
        {
            return new(new(unit, pos, pos));
        }

        switch (result) {
            // Cases where we reset movement
            case {
                ResultKind: NecoUnitActionResult.Kind.Failure,
                StateChange: NecoUnitActionOutcome.UnitTranslated translation
            }: {
                var unit = Field.GetUnit(uid, out var pos);
                PendingMovements[uid] = new(new(translation.Movement, pos, pos, result));
                break;
            }

            // Cases where things happen
            case { StateChange: NecoUnitActionOutcome.UnitTranslated translation }: {
                PendingMovements[uid] = new(new(translation.Movement, source: result));
                break;
            }

            default: {
                var unit = Field.GetUnit(uid, out var pos);
                PendingMovements[uid] = Default(unit, pos);
                break;
            }
        }
    }

    private static NecoUnitMovement? DecideCollisionWinner(UnitMovementPair unitPair)
    {
        if (unitPair.Collection.DistinctBy(u => u.NewPos).Count() > 1) {
            throw new NecoBowlException("units are not colliding");
        }

        /*
         * - Unit that is stationary
         * - Unit that is not holding ball (when other is holding ball)
         * - Unit with highest power
         * - Unit travelling vertically
         * - Unit traveling diagonally
         * - Bounce
         */

        if (unitPair.TryUnitWhereSingle(u => !u.IsChange, out var stationaryUnit, out var movingUnit)) {
            return stationaryUnit!;
        }

        // Holding ball
        if (unitPair.TryUnitWhereSingle(u => u.Unit.Tags.Contains(NecoUnitTag.TheBall),
                out var ballUnit,
                out var nonBallUnit)) {
            return ballUnit!;
        }

        {
            // Max power
            var groups = unitPair.Collection.GroupBy(m => m.Unit.Power).ToList();
            if (groups.Count > 1) {
                return groups.MaxBy(g => g.First().Unit.Power)!.First();
            }
        }

        {
            // Vertical
            var groups = unitPair.Collection.GroupBy(m => Math.Abs(m.Difference.Y)).ToList();
            if (groups.Count > 1) {
                return groups.MaxBy(g => g.First().Difference.Y)!.First();
            }
        }

        {
            // Diagonal
            if (unitPair.TryUnitWhereSingle(m => Math.Abs(m.Difference.X) + Math.Abs(m.Difference.Y) > 1,
                    out var diagonalMover,
                    out _)) {
                return diagonalMover!;
            }
        }

        return null;
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
