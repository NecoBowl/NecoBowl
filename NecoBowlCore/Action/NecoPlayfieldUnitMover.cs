using neco_soft.NecoBowlCore.Tags;

using NLog;

namespace neco_soft.NecoBowlCore.Action;

internal class NecoPlayfieldUnitMover
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly PlayfieldCollisionResolver CollisionResolver;
    private readonly List<NecoUnitMovement> MovementsList;

    private readonly List<NecoPlayfieldMutation> OutputMutations = new();

    private readonly NecoField Playfield;

    public NecoPlayfieldUnitMover(NecoField playfield, IEnumerable<NecoUnitMovement> movements)
    {
        Playfield = playfield;
        CollisionResolver = new(Playfield.AsReadOnly());
        MovementsList = movements.ToList();

        // Ensure movements actually correspond to the correct spaces
        if (MovementsList.Any(m => Playfield.GetUnit(m.OldPos).Id != m.UnitId)) {
            throw new PlayfieldMovementException();
        }
    }

    private IEnumerable<NecoPlayfieldMutation.MovementMutation> GetOutputMovements()
    {
        return OutputMutations
            .Where(mut => mut is NecoPlayfieldMutation.MovementMutation)
            .Cast<NecoPlayfieldMutation.MovementMutation>();
    }

    private NecoUnitMovement GetMovement(NecoUnitId uid)
    {
        return MovementsList.Single(m => m.UnitId == uid);
    }

    /// <summary>
    ///     Calculate collisions and apply movements for each unit on the field.
    /// </summary>
    public void MoveUnits(out List<NecoPlayfieldMutation> results)
    {
        if (OutputMutations.Any()) {
            throw new PlayfieldMovementException("cannot move units twice");
        }

        // Step through the movement process
        // The contents of the MovementList are not transferred over to the `mutations` list natively -- a movement
        //  mutation must be created for each one. If no movement mutation is created, then nothing will happen.

        AddEntriesForStationaryUnits();

        HandleFailureMovements();
        HandleSpaceSwaps();
        HandleSpaceConflicts();

        ReassignUnitSpaces();

        results = OutputMutations;
    }

    private void HandleSpaceConflicts()
    {
        uint conflictSpaceCount = 0;
        do {
            conflictSpaceCount = 0;
            foreach (var (pos, _) in Playfield.SpacePositions) {
                var targeters = MovementsList.Where(m => m.NewPos == pos).ToList();
                if (!targeters.Any()) {
                    continue;
                }

                if (targeters.Count > 1) {
                    OutputMutations.AddRange(CollisionResolver.ResolveSpaceConflict(targeters, out var winners));

                    foreach (var targeter in targeters.Where(m => !winners.Contains(m))) {
                        MovementsList.Remove(targeter);
                    }

                    if (winners.Count != targeters.Count) {
                        conflictSpaceCount++;
                    }
                }
            }
        } while (conflictSpaceCount > 0);
    }

    private void AddEntriesForStationaryUnits()
    {
        foreach (var (pos, unit) in Playfield.GetAllUnits()) {
            if (MovementsList.SingleOrDefault(m => m.UnitId == unit.Id) is null) {
                MovementsList.Add(new(unit, pos, pos));
            }
        }
    }

    private void HandleFailureMovements()
    {
        foreach (var failureMovement in FailureMovements()) {
            // TODO Implement the normalize function
            OutputMutations.Add(
                new NecoPlayfieldMutation.UnitBumps(failureMovement.UnitId, failureMovement.AsDirection()));
        }
    }

    private IEnumerable<NecoUnitMovement> FailureMovements()
    {
        return MovementsList.Where(m => m.Source is not null)
            .Select(m => m.Source!)
            .Where(m => m is not null && !Playfield.IsInBounds(m.NewPos));
    }

    private void HandleSpaceSwaps()
    {
        foreach (var pair in NecoUnitMovement.GetMovementPairs(MovementsList).Where(p => p.IsSpaceSwap())) {
            OutputMutations.AddRange(CollisionResolver.ResolveSpaceSwap(pair, out var winner));
            if (winner is not null) {
                MovementsList.Remove(pair.OtherMovement(winner));
            }
            else {
                MovementsList.RemoveAll(m => pair.Collection.Contains(m));
            }
        }
    }

    private void ReassignUnitSpaces()
    {
        if (Playfield.TempUnitZone.Any()) {
            throw new PlayfieldMovementException("TempUnitZone is polluted -- clear it before calling Reassign");
        }

        // Entries have been removed from MovementList over the course of the movement calculation.
        // We will re-add them with both positions as OldPos.
        AddEntriesForStationaryUnits();

        // Put everyone in the TempZone for lookup.
        foreach (var (pos, unit) in Playfield.GetAllUnits()) {
            Playfield.TempUnitZone.Add(unit);
        }

        // Clear the field.
        foreach (var movement in MovementsList) {
            Playfield[movement.OldPos] = Playfield[movement.OldPos] with { Unit = null };
        }

        // Stores mutations emitted during movement. We will add these after the movements are placed.
        var postMovementMutations = new List<NecoPlayfieldMutation.BaseMutation>();
        var movementDecisions = new List<NecoUnitMovement>();

        foreach (var movement in MovementsList) {
            if (movementDecisions.SingleOrDefault(m => m.NewPos == movement.NewPos) is { } otherUnit) {
                // There's a collision while reassigning unit spaces
                var otherUnitMovement = MovementsList.SingleOrDefault(m => m.UnitId == otherUnit.UnitId)
                 ?? new NecoUnitMovement(otherUnit, movement.NewPos, movement.NewPos);
                foreach (var mut in CollisionResolver.ResolveTransferConflict(
                             new(movement, otherUnitMovement),
                             out var conflictWinnner,
                             out var auxiliaryWinner)) {
                    postMovementMutations.Add(mut);
                    if (auxiliaryWinner is not null) {
                        movementDecisions.Add(auxiliaryWinner);
                    }

                    movementDecisions.Add(conflictWinnner);
                }
            }
            else {
                movementDecisions.Add(movement);
            }
        }

        // Turn the movement decisions into MovementMutations
        foreach (var movement in movementDecisions) {
            Playfield[movement.NewPos] = Playfield[movement.NewPos] with { Unit = movement.Unit };
            if (movement.IsChange) {
                if (OutputMutations
                    .Where(mut => mut is NecoPlayfieldMutation.MovementMutation)
                    .Cast<NecoPlayfieldMutation.MovementMutation>()
                    .Any(moveMut => moveMut.Subject == movement.UnitId)) {
                    Logger.Warn($"Ignoring extra movement for {movement.UnitId}");
                }
                else {
                    OutputMutations.Add(new NecoPlayfieldMutation.MovementMutation(movement));
                }
            }
        }

        foreach (var movement in MovementsList.Except(movementDecisions)) {
            Playfield[movement.OldPos] = Playfield[movement.OldPos] with { Unit = movement.Unit };
            if (movement.Source?.IsChange ?? false) {
                OutputMutations.Add(
                    new NecoPlayfieldMutation.UnitBumps(movement.UnitId, movement.Source.AsDirection()));
            }
        }

        OutputMutations.AddRange(postMovementMutations);
    }

    public class PlayfieldMovementException : Exception
    {
        public PlayfieldMovementException() { }
        public PlayfieldMovementException(string message) : base(message) { }
        public PlayfieldMovementException(string message, Exception inner) : base(message, inner) { }
    }
}

internal class PlayfieldCollisionResolver
{
    private readonly ReadOnlyNecoField Field;

    public PlayfieldCollisionResolver(ReadOnlyNecoField field)
    {
        Field = field;
    }

    public IEnumerable<NecoPlayfieldMutation.BaseMutation> ResolveSpaceSwap(
        UnitMovementPair pair,
        out NecoUnitMovement? winner)
    {
        if (!pair.IsSpaceSwap()) {
            throw new NecoPlayfieldUnitMover.PlayfieldMovementException();
        }

        var collection = new List<NecoPlayfieldMutation.BaseMutation>();

        if (pair.UnitsAreEnemies()) {
            // Opposing units
            if (pair.Movement1.Unit.CanAttackByMovement) {
                collection.Add(
                    new NecoPlayfieldMutation.UnitAttacks(
                        pair.Unit1.Id,
                        pair.Unit2.Id,
                        pair.Unit1.Power,
                        NecoPlayfieldMutation.UnitAttacks.Kind.SpaceSwap,
                        null));
            }

            winner = null;
        }
        else if (pair.Unit1.CanPickUp(pair.Unit2) && !pair.Unit2.CanPickUp(pair.Unit1)) {
            // ^ Don't use pair.PickupCanOccur because the pair order matters
            // Unit 1 can pick unit 2 up
            collection.Add(new NecoPlayfieldMutation.UnitPicksUpItem(pair.Unit1.Id, pair.Unit2.Id, pair.Movement1));
            winner = pair.Movement1;
        }
        else {
            // Friendly or neutral units 
            if (pair.PickupCanOccur(out var carrierUnit, out var itemUnit)) {
                // Cancel because this unit1 is getting picked up
                winner = carrierUnit;
                goto endCollision;
            }

            collection.Add(new NecoPlayfieldMutation.UnitBumps(pair.Unit1.Id, pair.Movement1.AsDirection()));
            winner = null;
        }

    endCollision:

        return collection;
    }

    public IEnumerable<NecoPlayfieldMutation.BaseMutation> ResolveSpaceConflict(
        IEnumerable<NecoUnitMovement> incomingMovements, out List<NecoUnitMovement> remainingMovements)
    {
        var list = new List<NecoPlayfieldMutation.BaseMutation>();

        remainingMovements = new();

        if (incomingMovements.GroupBy(m => m.NewPos).Count() > 1) {
            throw new NecoPlayfieldUnitMover.PlayfieldMovementException(
                "there is more than one destination position among the colliders");
        }

        var unitVictories = new Dictionary<NecoUnitMovement, int>();

        void IncrementVictoryCount(NecoUnitMovement movement)
        {
            unitVictories![movement] = unitVictories.ContainsKey(movement) ? unitVictories[movement]++ : 1;
        }

        NecoUnitMovement? finalWinner = null;

        var lastCount = int.MaxValue;
        var units = incomingMovements.ToList();

        while (finalWinner is null) {
            var friendlyCollision = false;
            foreach (var pair in NecoUnitMovement.GetMovementPairs(units)) {
                if (!pair.Movement1.IsChange) {
                    continue;
                }

                if (pair.UnitsCanFight()) {
                    list.Add(
                        new NecoPlayfieldMutation.UnitAttacks(
                            pair.Unit1.Id,
                            pair.Unit2.Id,
                            pair.Unit1.Power,
                            NecoPlayfieldMutation.UnitAttacks.Kind.SpaceConflict,
                            pair.Movement1.NewPos));
                }
                else {
                    if (pair.Unit1.CanPickUp(pair.Unit2) && !pair.Unit2.CanPickUp(pair.Unit1)) {
                        IncrementVictoryCount(pair.Movement1);
                        remainingMovements.Add(pair.Movement2);
                    }
                    else {
                        var winner = DecideCollisionWinner(pair, out _);
                        if (winner is not null) {
                            IncrementVictoryCount(winner);
                        }
                    }

                    friendlyCollision = true;
                }
            }

            if (!friendlyCollision) {
                break;
            }

            var winners = unitVictories.GroupBy(kv => kv.Value).MaxBy(g => g.Key)!.ToList();
            if (winners.Count == 1) {
                finalWinner = winners.First().Key;
            }
            else if (winners.Count > 0) {
                if (lastCount <= winners.Count) {
                    // The resolve process isn't going anywhere, bail out
                    finalWinner = winners.First().Key;
                }
                else {
                    // Prepare for next loop
                    units = winners.Select(kv => kv.Key).ToList();
                    lastCount = winners.Count;
                }
            }
        }

        if (finalWinner is not null) {
            remainingMovements.Add(finalWinner);
            var tempRemainingMovements = remainingMovements.ToList();

            // Find units that aren't the final winner and that don't already have their own mutation.
            foreach (var movement in units.Where(
                         m => !tempRemainingMovements.Contains(m) && m.IsChange
                          && !list.Any(mut => mut.Subject == m.UnitId))) {
                list.Add(new NecoPlayfieldMutation.UnitBumps(movement.UnitId, movement.AsDirection()));
            }
        }

        return list;
    }

    public IEnumerable<NecoPlayfieldMutation.BaseMutation> ResolveTransferConflict(
        UnitMovementPair pair,
        out NecoUnitMovement conflictWinnner,
        out NecoUnitMovement? auxiliaryWinner)
    {
        // We need to use a list to have out parameters.
        var list = new List<NecoPlayfieldMutation.BaseMutation>();

        if (pair.PickupCanOccur(out var carrierUnit, out var itemUnit)) {
            conflictWinnner = carrierUnit;
            list.Add(
                new NecoPlayfieldMutation.UnitPicksUpItem(conflictWinnner.UnitId, itemUnit.UnitId, conflictWinnner));
            auxiliaryWinner = itemUnit;
            return list;
        }

        throw new($"unhandled transfer conflict between {pair.Unit1} and {pair.Unit2}");
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

        {
            // Max power
            var groups = unitPair.Collection.GroupBy(m => m.Unit.Power).ToList();
            if (groups.Count > 1) {
                var sorted = groups.Select(g => g.First()).OrderByDescending(m => m.Unit.Power).ToList();
                other = sorted.Last();
                return sorted.First();
            }
        }

        other = null;
        return null;
    }
}
