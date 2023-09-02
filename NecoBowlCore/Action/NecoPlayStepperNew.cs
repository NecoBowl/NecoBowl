using System.Diagnostics.CodeAnalysis;

using neco_soft.NecoBowlCore.Tags;

using NLog;

namespace neco_soft.NecoBowlCore.Action;

internal class NecoPlayStepperNew
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly List<NecoPlayfieldMutation.MovementMutation> DeferredMovements = new();

    private readonly NecoField Field;
    private readonly List<NecoPlayfieldMutation> MutationHistory = new();

    private readonly Dictionary<NecoUnitId, NecoPlayfieldMutation.MovementMutation> PendingMovements = new();
    private readonly List<NecoPlayfieldMutation.BaseMutation> PendingMutations = new();

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

        PopUnitActions(out var chainedActions);

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

            // Add movements/mutations from multi-actions
            foreach (var (id, action) in chainedActions.Where(kv => kv.Value is not null)) {
                EnqueueMutationFromAction(id, action!.Result(id, Field.AsReadOnly()));
                chainedActions[id] = action.Next;
            }
        }

        foreach (var entry in stepMutations) {
            Logger.Debug(entry);
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

        AddPreMovementMutations();
    }

    private void ProcessPreMovementMutations()
    {
        foreach (var mutation in PendingMutations) {
            mutation.PreMovementMutate(Field, new(PendingMovements, PendingMutations));
        }
    }

    private void ConsumeMovements(out List<NecoPlayfieldMutation> resultantMutations)
    {
        resultantMutations = new();
        var deferredMovements = new List<NecoPlayfieldMutation.MovementMutation>();

        // Split the movements into two groups; one that has no movement, and the other that has attempted/successful movements.
        var finalMovements = PendingMovements.Values.Where(m => m.Movement.IsChangeInSource).ToList();
        var unitBuffer = new Dictionary<NecoUnitId, NecoUnitMovement>();
        foreach (var moveMut in finalMovements) {
            // For any unit that is going to move, place them in the movement buffer.
            var unit = moveMut.Movement.Unit;
            var movementMutation = PendingMovements[moveMut.Subject];
            var movement = movementMutation.Movement;
            unitBuffer[unit.Id] = movement;
            Field[movement.OldPos] = Field[movement.OldPos] with { Unit = null };
            PendingMovements.Remove(movementMutation.Subject);
        }

        // Perform each movement.
        foreach (var (uid, movement) in unitBuffer.Select(kv => (kv.Key, kv.Value))) {
            // Sanity check for collisions that didn't get handled
            var collisionError
                = unitBuffer.Values.Where(m => m.NewPos == movement.NewPos && m != movement && movement.IsChange);
            if (collisionError.Any() || Field[movement.NewPos].Unit is not null) {
                if (Field[movement.NewPos].Unit is { } newPosUnit && newPosUnit != movement.Unit) {
                    if (movement.Unit.Tags.Contains(NecoUnitTag.Carrier)
                     && newPosUnit.Tags.Contains(NecoUnitTag.Item)) {
                        Field.TempUnitZone.Add(newPosUnit);
                        PendingMutations.Add(
                            new NecoPlayfieldMutation.UnitPicksUpItem(movement.UnitId, newPosUnit.Id, movement));
                    }
                    else {
                        throw new NecoPlayfieldMutationException("collision in the unit buffer");
                    }
                }
            }

            // Update the unit position
            Field[movement.NewPos] = Field[movement.NewPos] with { Unit = movement.Unit };

            if (movement.IsChange) {
                resultantMutations.Add(new NecoPlayfieldMutation.MovementMutation(movement));
            }
            else if (movement.Source!.StateChange is NecoUnitActionOutcome.UnitTranslated source) {
                if (movement.Source?.ResultKind == NecoUnitActionResult.Kind.Failure) {
                    // Add bump event
                    if (!PendingMutations.Any(m => m.Subject == uid)) {
                        resultantMutations.Add(
                            new NecoPlayfieldMutation.UnitBumps(
                                uid,
                                source.Movement.AsDirection()));
                    }
                }
                else if (!movement.IsChange) {
                    // The unit had a legal move but was blocked
                    deferredMovements.Add(new(source.Movement));
                }
                else {
                    Logger.Warn($"unknown failure for movement: {movement.Source?.StateChange}");
                }
            }
        }

        DeferredMovements.Clear();
        DeferredMovements.AddRange(deferredMovements);
        PendingMovements.Clear();
        foreach (var m in DeferredMovements) {
            PendingMovements[m.Subject] = m;
        }
    }

    private void AddPreMovementMutations()
    {
        // Case: Unit with Pusher
        // TAGIMPL:Pusher
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
        var collisionOverrides = new List<NecoUnitMovement>();

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
                if (UpdatePendingMutationsBySpaceConflict(mut.Movement, spaceSwapMovements, collisionOverrides)) {
                    movementsToReset.Add(mut);
                }
            }

            foreach (var moveMut in movementsToReset) {
                PendingMovements[moveMut.Subject] = new(new(moveMut.Movement, moveMut.OldPos));
            }
        } while (GetConflicts(collisionOverrides).Any());
    }

    private IEnumerable<List<NecoPlayfieldMutation.MovementMutation>> GetConflicts(
        IEnumerable<NecoUnitMovement> forceCollisions)
    {
        return PendingMovements
            .ExceptBy(forceCollisions, kv => kv.Value.Movement)
            .GroupBy(kv => kv.Value.Movement.NewPos)
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
                                                           spaceSwapMutations,
                                                       List<NecoUnitMovement> collisionOverrides)
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
            if (unitPair.UnitsCanFight() && movement.IsChange
             && !spaceSwapMutations.Any(mut => mut.Subject == movement.UnitId)) {
                // TAGIMPL:Defender
                if (unitPair.UnitWithTag(NecoUnitTag.Defender, out _) == movement) {
                    shouldReset = true;
                }
                else {
                    PendingMutations.Add(
                        new NecoPlayfieldMutation.UnitAttacks(
                            Field.AsReadOnly(),
                            movement.UnitId,
                            conflict.UnitId,
                            NecoPlayfieldMutation.UnitAttacks.Kind.SpaceConflict,
                            movement.NewPos));
                    shouldReset = true;
                }
            }
            else {
                // TAGIMPL:Carrier
                if (unitPair.TryGetUnitsBy(
                        m => m.Unit.Tags.Contains(NecoUnitTag.Item),
                        m => m.Unit.Tags.Contains(NecoUnitTag.Carrier),
                        out var itemUnit,
                        out var carrierUnit)) {
                    if (carrierUnit == movement) {
                        // Pickup occurs
                        collisionOverrides.Add(movement);
//                        shouldReset = true;
                        goto DoneCheckingUnitPair;
                    }
                }

                // Friendly unit conflict 
                var collisionWinner = DecideCollisionWinner(unitPair, out var collisionLoser);
                if (collisionWinner == movement) {
                    // this unit is the collision winner
                    // TAGIMPL:Carrier
                    if (collisionLoser!.Unit.Inventory.Any(i => i.Tags.Contains(NecoUnitTag.TheBall))
                     && collisionWinner.Unit.Tags.Contains(NecoUnitTag.Carrier)) {
                        // force handoff 
                        var ballItem = collisionLoser.Unit.Inventory.Single(i => i.Tags.Contains(NecoUnitTag.TheBall));
                        PendingMutations.Add(
                            new NecoPlayfieldMutation.UnitHandsOffItem(
                                collisionLoser.UnitId,
                                collisionWinner.UnitId,
                                ballItem.Id));
                    }
                }
                else {
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
            // TAGIMPL:Defender
            if (unitPair.UnitWithTag(NecoUnitTag.Defender, out _) != movement) {
                PendingMutations.Add(
                    new NecoPlayfieldMutation.UnitAttacks(
                        Field.AsReadOnly(),
                        movement.UnitId,
                        swap.UnitId,
                        NecoPlayfieldMutation.UnitAttacks.Kind.SpaceSwap));
            }
        }
        else {
            // Friendly space swappers will just bump off each other.
            PendingMutations.Add(new NecoPlayfieldMutation.UnitBumps(movement.UnitId, swap.AsDirection()));
        }

        return true;
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
                PendingMovements[uid] = new(new(translation.Movement, pos, pos, result));
                break;
            }

            // Cases where things happen
            case { StateChange: NecoUnitActionOutcome.UnitTranslated translation }: {
                PendingMovements[uid] = new(new(translation.Movement, source: result));
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

    private static NecoUnitMovement? DecideCollisionWinner(UnitMovementPair unitPair, out NecoUnitMovement? other)
    {
        if (unitPair.Collection.DistinctBy(u => u.NewPos).Count() > 1) {
            throw new NecoBowlException("units are not colliding");
        }

        /*
         * - Unit that is stationary
         * - Unit with Bossy
         * - Unit with Carrier (when other is holding ball)
         * - Unit that is holding ball
         * - Unit travelling vertically
         * - Unit traveling diagonally
         * - Unit with highest power
         * - Bounce
         */

        // Stationary unit
        if (unitPair.TryUnitWhereSingle(u => !u.IsChange, out var stationaryUnit, out var movingUnit)) {
            other = movingUnit!;
            return stationaryUnit!;
        }

        // Unit with Bossy
        if (unitPair.TryUnitWhereSingle(
                u => u.Unit.Tags.Contains(NecoUnitTag.Bossy),
                out var bossyUnit,
                out var otherUnit)) {
            other = otherUnit!;
            return bossyUnit!;
        }

        // Forced handoff interaction
        if (unitPair.TryUnitWhereSingle(
                m => m.Unit.Inventory.Any(u => u.Tags.Contains(NecoUnitTag.TheBall)),
                out var ballHolder,
                out var nonBallHolder)) {
            // TAGIMPL:Carrier
            // TAGIMPL:Butterfingers
            if (nonBallHolder!.Unit.Tags.Contains(NecoUnitTag.Carrier)
             && !nonBallHolder.Unit.Tags.Contains(NecoUnitTag.Butterfingers)) {
                other = ballHolder!;
                return nonBallHolder!;
            }
        }

        // Unit holding ball
        if (unitPair.TryUnitWhereSingle(
                u => u.Unit.Tags.Contains(NecoUnitTag.TheBall),
                out var ballUnit,
                out var nonBallUnit)) {
            other = nonBallUnit!;
            return ballUnit!;
        }

        {
            // Max power
            var groups = unitPair.Collection.GroupBy(m => m.Unit.Power).ToList();
            if (groups.Count > 1) {
                var sorted = groups.Select(g => g.First()).OrderByDescending(m => m.Unit.Power).ToList();
                other = sorted.Last();
                return sorted.First();
            }
        }

        {
            // Vertical
            var groups = unitPair.Collection.GroupBy(m => Math.Abs(m.Difference.X)).ToList();
            if (groups.Count > 1) {
                var sorted = groups.Select(g => g.First()).OrderBy(m => Math.Abs(m.Difference.X));
                other = sorted.Last();
                return sorted.First(); // lowest X-difference would be vertical
            }
        }

        {
            // Diagonal
            if (unitPair.TryUnitWhereSingle(
                    m => Math.Abs(m.Difference.X) + Math.Abs(m.Difference.Y) > 1,
                    out var diagonalMover,
                    out var nonDiagonalMover)) {
                other = nonDiagonalMover!;
                return diagonalMover!;
            }
        }

        other = null;
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
