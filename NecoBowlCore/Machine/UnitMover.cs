using System.Collections;
using System.Collections.ObjectModel;
using NecoBowl.Core.Machine;
using NecoBowl.Core.Tags;
using NLog;

namespace NecoBowl.Core.Sport.Play;

internal class UnitMover
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly Dictionary<Unit, TransientUnit> MovementsList;
    private readonly IMutationReceiver MutationReceiver;

    private readonly Playfield Playfield;

    public UnitMover(
        IMutationReceiver receiver, Playfield playfield, IEnumerable<TransientUnit> movements)
    {
        MutationReceiver = receiver;
        Playfield = playfield;
        MovementsList = movements.ToDictionary(m => m.Unit, m => m);

        // Ensure movements actually correspond to the correct spaces
        if (MovementsList.Values.Any(m => Playfield.GetUnit(m.OldPos).Id != m.UnitId)) {
            throw new PlayfieldMovementException();
        }
    }

    /// <summary>Calculate collisions and apply movements for each unit on the field.</summary>
    public void MoveUnits(out IReadOnlyCollection<TransientUnit> resultantMovements)
    {
        AddEntriesForStationaryUnits();
        FixOutOfBoundsMovements();

        HandleSpaceSwaps();

        // Ensure all units have a movement.
        foreach (var (pos, unit) in Playfield.GetAllUnits()) {
            if (!MovementsList.ContainsKey(unit)) {
                throw new InvalidOperationException($"unit {unit} is missing a movement");
            }
        }

        // Ensure there are no invalid movements.
        if (MovementsList.Values.GroupBy(m => m.OldPos).Any(g => g.Count() > 1)) {
            throw new PlayfieldMovementException("multiple units on one starting space");
        }

        HandleSpaceConflicts();

        // Update field with new movements.
        MoveUnitsOnField();

        resultantMovements = MovementsList.Values;
    }

    private void FixOutOfBoundsMovements()
    {
        foreach (var outOfBounds in MovementsList.Values.Where(m => !Playfield.IsInBounds(m.NewPos))) {
            MovementsList[outOfBounds.Unit] = outOfBounds.WithoutMovement();
            MutationReceiver.BufferMutation(new UnitBumps(outOfBounds.UnitId, outOfBounds.AsDirection()));
        }
    }

    private void MoveUnitsOnField()
    {
        foreach (var (pos, space) in Playfield.SpacePositions) {
            Playfield[pos] = space with { Unit = null };
        }

        foreach (var (unit, movement) in MovementsList) {
            Playfield[movement.NewPos] = Playfield[movement.NewPos] with { Unit = unit };
        }
    }

    private void HandleSpaceConflicts()
    {
        var transientField = new TransientPlayfield(Playfield.AsReadOnly(), MovementsList.Values);
        foreach (var (unit, change) in transientField.GetResetMovements()) {
            // Create combat / bump events 
            switch (change.Reason) {
                case SpaceImmigrantRemovalReason.Superseded superseded: {
                    MutationReceiver.BufferMutation(new UnitBumps(unit.Id, change.Movement.AsDirection()));
                    break;
                }

                case SpaceImmigrantRemovalReason.Removed removal: {
                    break;
                }

                case SpaceImmigrantRemovalReason.Combat combat: {
                    foreach (var opponent in combat.Other) {
                        MutationReceiver.BufferMutation(
                            new UnitAttacks(
                                unit.Id, opponent.Id, unit.Power, UnitAttacks.Kind.SpaceConflict,
                                change.Movement.NewPos));
                    }

                    break;
                }
            }

            MovementsList[change.Movement.Unit] = change.Movement.WithoutMovement();
        }
    }

    private void AddEntriesForStationaryUnits()
    {
        foreach (var (pos, unit) in Playfield.GetAllUnits()) {
            if (MovementsList.Keys.All(m => m != unit)) {
                MovementsList[unit] = new(pos, pos, unit);
            }
        }
    }

    private void HandleSpaceSwaps()
    {
        var startLength = MovementsList.Count;

        var spaceSwaps = PlayfieldCollisionResolver.GetPairCombinations(MovementsList.Values)
            .Where(p => p.IsSpaceSwap());
        foreach (var pair in spaceSwaps) {
            // Just reset movements for now
            foreach (var movement in pair) {
                MovementsList[movement.Unit] = movement;
            }
        }

        if (MovementsList.Count != startLength) {
            throw new InvalidOperationException("error resetting swap movements");
        }
    }

#if false
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
                // There's a collision while reassigning unit spaces.
                // This (intentionally) occurs when two units are moving onto the space at once -- for example, a unit
                //  moving onto the ball's space as it gets pushed there.
                var otherUnitMovement = MovementsList.SingleOrDefault(m => m.UnitId == otherUnit.UnitId)
                    ?? new NecoUnitMovement(otherUnit, movement.NewPos, movement.NewPos);
                var movementPair = new UnitMovementPair(movement, otherUnitMovement);
                var resolveConflictMutations = CollisionResolver.ResolveTransferConflict(
                    movementPair,
                    out var conflictWinnner,
                    out var auxiliaryWinner);

                foreach (var mut in resolveConflictMutations) {
                    postMovementMutations.Add(mut);
                }

                movementDecisions.Add(conflictWinnner);
                if (auxiliaryWinner is not null) {
                    movementDecisions.Add(auxiliaryWinner);
                }
                else {
                    // Reset the collider's movement
                    var loser = movementPair.OtherMovement(conflictWinnner);
                    movementDecisions.Add(new(loser, loser.OldPos, source: loser));
                }
            }
            else {
                movementDecisions.Add(movement);
            }
        }

        // Check for units with multiple movement decisions
        foreach (var unitMovements in movementDecisions.ToList().GroupBy(m => m.UnitId)) {
            foreach (var extraMovement in unitMovements.Skip(1)) {
                Logger.Warn($"Ignoring extra movement for {extraMovement.UnitId}");
                movementDecisions.Remove(extraMovement);
            }
        }

        // Turn the movement decisions into MovementMutations
        foreach (var movement in movementDecisions) {
            Playfield[movement.NewPos] = Playfield[movement.NewPos] with { Unit = movement.Unit };
            if (!movement.IsChange) {
                continue;
            }

            MutationReceiver.LogMutation(new NecoPlayfieldMutation.MovementMutation(movement));
        }

        foreach (var movement in MovementsList.Except(movementDecisions)) {
            Playfield[movement.OldPos] = Playfield[movement.OldPos] with { Unit = movement.Unit };
            if (movement.Source?.IsChange ?? false) {
                MutationReceiver.BufferMutation(
                    new NecoPlayfieldMutation.UnitBumps(movement.UnitId, movement.Source.AsDirection()));
            }
        }

        postMovementMutations.ForEach(MutationReceiver.BufferMutation);
    }
#endif

    public class PlayfieldMovementException : Exception
    {
        public PlayfieldMovementException() { }
        public PlayfieldMovementException(string message) : base(message) { }
        public PlayfieldMovementException(string message, Exception inner) : base(message, inner) { }
    }
}

internal class PlayfieldCollisionResolver
{
    public enum CollisionVictoryReasonLevel
    {
        Stationary = -100,
        Bossy = -90,
        Handoff = -80,
        BallHolder = -70,
        VerticalMovement = -60,
        HorizontalMovement = -50,
        Bounce = 0
    }

    private readonly ReadOnlyPlayfield Field;
    private readonly IMutationReceiver MutationReceiver;

    public PlayfieldCollisionResolver(ReadOnlyPlayfield field, IMutationReceiver mutationReceiver)
    {
        Field = field;
        MutationReceiver = mutationReceiver;
    }

    public void ResolveSpaceSwap(
        UnitMovementPair pair, out TransientUnit? winner)
    {
        if (!pair.IsSpaceSwap()) {
            throw new UnitMover.PlayfieldMovementException();
        }

        if (pair.UnitsAreEnemies()) {
            // Opposing units
            if (pair.Movement1.Unit.CanAttackByMovement) {
                MutationReceiver.BufferMutation(
                    new UnitAttacks(
                        pair.Unit1.Id,
                        pair.Unit2.Id,
                        pair.Unit1.Power,
                        UnitAttacks.Kind.SpaceSwap,
                        null));
            }
            else {
                MutationReceiver.BufferMutation(new UnitBumps(pair.Unit1.Id, pair.Movement1.AsDirection()));
            }

            winner = null;
        }
        else if (pair.Unit1.CanPickUp(pair.Unit2) && !pair.Unit2.CanPickUp(pair.Unit1)) {
            // ^ Don't use pair.PickupCanOccur because the pair order matters
            // Unit 1 can pick unit 2 up
            MutationReceiver.BufferMutation(new UnitPicksUpItem(pair.Unit1.Id, pair.Unit2.Id));
            winner = pair.Movement1;
        }
        else {
            // Friendly or neutral units 
            if (pair.PickupCanOccur(out var carrierUnit, out var itemUnit)) {
                // Cancel because this unit1 is getting picked up
                winner = carrierUnit;
                return;
            }

            MutationReceiver.BufferMutation(new UnitBumps(pair.Unit1.Id, pair.Movement1.AsDirection()));
            winner = null;
        }
    }


#if FALSE
    public void ResolveSpaceConflict(
        IEnumerable<NecoUnitMovement> incomingMovements,
        out IReadOnlySet<NecoUnitMovement> remainingMovementsOutput)
    {
        incomingMovements = incomingMovements.ToList();

        if (incomingMovements.GroupBy(m => m.NewPos).Count() > 1) {
            throw new InvalidOperationException("there is more than one destination position among the colliders");
        }

        var mutationList = new List<NecoPlayfieldMutation.BaseMutation>();

        var unitVictories = new Dictionary<NecoUnitMovement, int>();

        NecoUnitMovement? finalWinner = null;

        var lastCount = int.MaxValue;
        var bumpCandidates = incomingMovements.ToList();

        // If there's a stationary unit, it wins by default.

        while (finalWinner is null) {
            var movementPermutations = incomingMovements.GetPermutations()
                .Select(tup => tup.Splat((m1, m2) => new UnitMovementPair(m1, m2)));

            foreach (var pair in movementPermutations) {
                if (pair.UnitsAreEnemies()) {
                    // Enemy units are converging onto the same space.
                    if (pair.Unit1.CanAttackOther(pair.Unit2)) {
                        MutationReceiver.BufferMutation(
                            new NecoPlayfieldMutation.UnitAttacks(
                                pair.Unit1.Id,
                                pair.Unit2.Id,
                                pair.Unit1.Power,
                                NecoPlayfieldMutation.UnitAttacks.Kind.SpaceConflict,
                                pair.Movement1.NewPos));
                        bumpCandidates.Remove(pair.Movement1);
                    }
                    // Otherwise enemies but they can't attack (i.e. the Defender tag)
                }
                else {
                    // Friendly/neutral units are converging onto the same space.
                    if (pair.Unit1.CanPickUp(pair.Unit2) && !pair.Unit2.CanPickUp(pair.Unit1)) {
                        IncrementVictoryCount(pair.Movement1);
                        bumpCandidates.Remove(pair.Movement1);
//                        remainingMovements.Add(pair.Movement2);
                    }
                    else {
                        var winner = GetCollisionScores(pair, out _);
                        if (winner == pair.Movement1) {
                            IncrementVictoryCount(winner);
                        }
                    }
                }
            }

            if (!unitVictories.Any()) {
                break;
            }

            var winners = unitVictories.GroupBy(kv => kv.Value).MaxBy(g => g.Key)!.ToList();
            switch (winners.Count) {
                case 1:
                case > 0 when lastCount <= winners.Count:
                    finalWinner = winners.First().Key;
                    break;
                case > 0:
                    // Prepare for next loop
                    incomingMovements = winners.Select(kv => kv.Key).ToHashSet();
                    lastCount = winners.Count;
                    break;
            }

            foreach (var movement in winners.Select(kv => kv.Key)) {
                bumpCandidates.Remove(movement);
            }
        }

        foreach (var bump in bumpCandidates) {
            if (bump.IsChange) {
                MutationReceiver.BufferMutation(new NecoPlayfieldMutation.UnitBumps(bump.UnitId, bump.AsDirection()));
            }
        }

        if (finalWinner is not null) {
            remainingMovements.Add(finalWinner);

            // Find units that aren't the final winner and that don't already have their own mutation.
            var bumpingUnits = incomingMovements.Where(
                m => !remainingMovements.Contains(m) && m.IsChange
                    && mutationList.All(mut => mut.Subject != m.UnitId));

            // Handoffs
            foreach (var movement in bumpingUnits) {
                mutationList.Add(new NecoPlayfieldMutation.UnitBumps(movement.UnitId, movement.AsDirection()));

                if (movement.Unit.HandoffItem() is not null) {
                    mutationList.Add(
                        new NecoPlayfieldMutation.UnitHandsOffItem(
                            movement.UnitId, finalWinner.UnitId, movement.Unit.HandoffItem()!.Id));
                }
            }
        }

        mutationList.ForEach(MutationReceiver.BufferMutation);
        remainingMovementsOutput = remainingMovements.ToHashSet();
        return;

        void IncrementVictoryCount(NecoUnitMovement movement)
        {
            unitVictories[movement] = unitVictories.TryGetValue(movement, out var count) ? count + 1 : 1;
        }
    }
#endif

    public static IDictionary<TransientUnit, CollisionVictoryReasonLevel> GetCollisionScores(
        UnitMovementPair unitPair)
    {
        if (unitPair.Collection.DistinctBy(u => u.NewPos).Count() > 1) {
            throw new NecoBowlException("units are not colliding");
        }

        /*
         * - Unit that can absorb the other (when the other cannot absorb the unit)
         * - Unit that is stationary
         * - Unit with Bossy
         * - Unit with Carrier (when other is holding ball)
         * - Unit that is holding ball
         * - Unit travelling vertically
         * - Unit traveling diagonally
         * - Bounce
         */

        // Stationary unit
        {
            if (unitPair.TryUnitWhereSingle(u => !u.IsChange, out var stationaryUnit, out _)) {
                return Create(stationaryUnit, CollisionVictoryReasonLevel.Stationary);
            }
        }

        // Unit with Bossy
        if (unitPair.TryUnitWhereSingle(
                u => u.Unit.Tags.Contains(NecoUnitTag.Bossy),
                out var bossyUnit,
                out _)) {
            return Create(bossyUnit, CollisionVictoryReasonLevel.Bossy);
        }

        // Forced handoff interaction
        if (unitPair.TryUnitWhereSingle(
                m => m.Unit.Inventory.Any(u => u.Tags.Contains(NecoUnitTag.TheBall)),
                out var ballHolder,
                out var nonBallHolder)) {
            // TAGIMPL:Carrier
            // TAGIMPL:Butterfingers
            if (nonBallHolder.Unit.Tags.Contains(NecoUnitTag.Carrier)
                && !nonBallHolder.Unit.Tags.Contains(NecoUnitTag.Butterfingers)) {
                return Create(nonBallHolder, CollisionVictoryReasonLevel.Handoff);
            }
        }


        // Unit holding ball
        if (unitPair.TryUnitWhereSingle(
                u => u.Unit.Tags.Contains(NecoUnitTag.TheBall),
                out var ballUnit,
                out _)) {
            return Create(ballUnit, CollisionVictoryReasonLevel.BallHolder);
        }

        {
            // Vertical
            var groups = unitPair.Collection.GroupBy(m => Math.Abs(m.Difference.X)).ToList();
            if (groups.Count > 1) {
                var sorted = groups.Select(g => g.Single()).OrderBy(m => Math.Abs(m.Difference.X)).ToList();
                return Create(sorted.First(), CollisionVictoryReasonLevel.VerticalMovement);
            }
        }

        {
            // Horizontal
            if (unitPair.TryUnitWhereSingle(
                    m => Math.Abs(m.Difference.X) > 0 && Math.Abs(m.Difference.Y) == 0,
                    out var horizontalMover,
                    out _)) {
                return Create(horizontalMover, CollisionVictoryReasonLevel.HorizontalMovement);
            }
        }

        // They both bounce off each other.
        return Create(unitPair.Movement1, CollisionVictoryReasonLevel.Bounce);

        IDictionary<TransientUnit, CollisionVictoryReasonLevel> Create(
            TransientUnit movement, CollisionVictoryReasonLevel level)
        {
            return new Dictionary<TransientUnit, CollisionVictoryReasonLevel> {
                [movement] = level,
                [unitPair.OtherMovement(movement)] = CollisionVictoryReasonLevel.Bounce
            };
        }
    }

    public static IReadOnlyCollection<UnitMovementPair> GetPairCombinations(IEnumerable<TransientUnit> movements)
    {
        movements = movements.ToList();

        var output = new List<UnitMovementPair>();
        foreach (var movement in movements) {
            foreach (var other in movements) {
                if (movement != other) {
                    var pair = new UnitMovementPair(movement, other);
                    if (!output.Any(p => pair.IsSameUnitsAs(p))) {
                        output.Add(pair);
                    }
                }
            }
        }

        return output;
    }
}

/// <summary>Represents the spaces on a playfield. Unlike <see cref="Playfield" />, the spaces can store multiple units.</summary>
internal class TransientPlayfield : ITransientSpaceContentsGetter
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private readonly ReadOnlyDictionary<Unit, SpaceImmigrantRemoval> ResetReasons;
    private readonly ReadOnlyDictionary<Vector2i, Space> Spaces;

    public TransientPlayfield(ReadOnlyPlayfield field, IEnumerable<TransientUnit> movements)
    {
        movements = movements.ToList();
        var spaces = new Dictionary<Vector2i, Space>();
        var resetReasons = new Dictionary<Unit, SpaceImmigrantRemoval>();

        // Set up transient space array
        for (var i = 0; i < field.GetBounds().X; i++) {
            for (var j = 0; j < field.GetBounds().Y; j++) {
                var pos = new Vector2i(i, j);
                var movementsHere = movements.Where(m => m.NewPos == pos);
                spaces[pos] = new(pos, movementsHere, this);
            }
        }

        // Now we begin the space reassignment algorithm.
        // It repeats until every space has no conflict.
        while (spaces.Select(kv => kv.Value).Any(space => space.HasConflict)) {
            // We will apply the changes suggested by GetRemovedImmigrants to this dict.
            var unstableSpaces = spaces.ToDictionary(kv => kv.Key, _ => new List<TransientUnit>());

            foreach (var (pos, space) in spaces) {
                foreach (var removal in space.GetRemovedImmigrants()) {
                    var movement = removal.Movement.WithoutMovement();
                    unstableSpaces[movement.OldPos].Add(movement);
                    if (resetReasons.Keys.Any(u => u == movement.Unit)) {
                        Logger.Warn($"Multiple reset reasons for {removal.Movement.Unit}");
                    }

                    resetReasons[movement.Unit] = removal;
                }

                foreach (var survivor in space.GetRemainingImmigrants()) {
                    unstableSpaces[pos].Add(survivor);
                }
            }

            spaces = unstableSpaces.ToDictionary(kv => kv.Key, kv => new Space(kv.Key, unstableSpaces[kv.Key], this));
        }

        // User can access the results via Spaces.
        Spaces = new(spaces);
        ResetReasons = new(resetReasons);
    }

    public IReadOnlyCollection<TransientUnit> GetMovementsFrom(Vector2i pos)
    {
        return GetAllMovements().Where(m => m.OldPos == pos).ToList();
    }

    public IReadOnlyDictionary<Unit, SpaceImmigrantRemoval> GetResetMovements()
    {
        return ResetReasons;
    }

    private IEnumerable<TransientUnit> GetAllMovements()
    {
        return Spaces.SelectMany(kv => kv.Value.GetAllImmigrants(), (kv, movement) => movement);
    }

    /// <summary>
    /// A space during movement calculation. Stores multiple "immigrants", which are the units attempting to move onto this
    /// space.
    /// </summary>
    private class Space
    {
        private readonly IReadOnlyList<TransientUnit> Immigrants;
        private readonly ITransientSpaceContentsGetter Playfield;
        private readonly Vector2i Position;

        public Space(
            Vector2i position, IEnumerable<TransientUnit> immigrants, ITransientSpaceContentsGetter playfield)
        {
            Immigrants = immigrants.ToList().AsReadOnly();
            Position = position;
            Playfield = playfield;

            if (Immigrants.Any(movement => movement.NewPos != Position)) {
                throw new InvalidOperationException("movement destination differs from space position");
            }
        }

        public bool HasConflict => Immigrants.Count > 1;

        public IReadOnlyCollection<TransientUnit> GetAllImmigrants()
        {
            return Immigrants;
        }

        public IReadOnlyCollection<TransientUnit> GetRemainingImmigrants()
        {
            return Immigrants.Except(GetRemovedImmigrants().Select(removal => removal.Movement)).ToList();
        }

        /// <summary>
        /// Get all the units that must be removed from this space before it can be turned back into a <see cref="Playfield" />
        /// space.
        /// </summary>
        public IReadOnlyCollection<SpaceImmigrantRemoval> GetRemovedImmigrants()
        {
            var combats = new Dictionary<TransientUnit, List<TransientUnit>>();
            var bestVictoryLevels =
                new Dictionary<TransientUnit, PlayfieldCollisionResolver.CollisionVictoryReasonLevel>();

            var combinations = PlayfieldCollisionResolver.GetPairCombinations(Immigrants);
            foreach (var pair in combinations) {
                foreach (var movement in pair) {
                    if (movement.Unit.CanAttackOther(pair.OtherMovement(movement).Unit)) {
                        combats[movement] = new() { pair.OtherMovement(movement) };
                    }
                }

                foreach (var (movement, level) in PlayfieldCollisionResolver.GetCollisionScores(pair)) {
                    if ((int)bestVictoryLevels.GetValueOrDefault(
                            movement, PlayfieldCollisionResolver.CollisionVictoryReasonLevel.Bounce) > (int)level) {
                        bestVictoryLevels[movement] = level;
                    }
                }
            }

            var removals = new List<SpaceImmigrantRemoval>();

            if (combats.Any() || bestVictoryLevels.Any()) {
                var winners = bestVictoryLevels.Any()
                    ? new WinnerList(
                        bestVictoryLevels
                            .GroupBy(kv => (int)kv.Value, kv => kv.Key)
                            .OrderBy(g => g.Key))
                    : null;
                foreach (var immigrant in Immigrants) {
                    if (winners?.Contains(immigrant) ?? false) {
                        continue;
                    }

                    if (combats.TryGetValue(immigrant, out var targets)) {
                        removals.Add(
                            new(
                                immigrant,
                                new SpaceImmigrantRemovalReason.Combat(targets.Select(m => m.Unit))));
                    }
                    else {
                        removals.Add(new(immigrant, new SpaceImmigrantRemovalReason.Superseded()));
                    }
                }
            }

            return removals;
        }
    }
}

internal class SpaceImmigrantRemoval
{
    public readonly TransientUnit Movement;
    public readonly SpaceImmigrantRemovalReason Reason;

    public SpaceImmigrantRemoval(TransientUnit movement, SpaceImmigrantRemovalReason reason)
    {
        Movement = movement;
        Reason = reason;
    }
}

internal abstract class SpaceImmigrantRemovalReason
{
    public sealed class Combat : SpaceImmigrantRemovalReason
    {
        public readonly IEnumerable<Unit> Other;

        public Combat(IEnumerable<Unit> other)
        {
            Other = other;
        }
    }

    public sealed class Superseded : SpaceImmigrantRemovalReason
    {
    }

    public sealed class Removed : SpaceImmigrantRemovalReason
    {
    }
}

internal interface ITransientSpaceContentsGetter
{
    public IReadOnlyCollection<TransientUnit> GetMovementsFrom(Vector2i pos);
}

internal class WinnerList : IEnumerable<TransientUnit>
{
    public readonly IReadOnlyCollection<TransientUnit> AuxiliaryWinners;

    public readonly TransientUnit Winner;

    public WinnerList(IEnumerable<IGrouping<int, TransientUnit>> collection)
    {
        collection = collection.ToList();

        // Validate.
        if (!collection.Any()) {
            throw new ArgumentException("at least one grouping is required for a winner list");
        }

        if (collection.First().Count() != 1) {
            throw new ArgumentException("first winner group can only have one unit");
        }

        Winner = collection.First().Single();

        var secondGroup = collection.Skip(1).FirstOrDefault();
        if (collection.Count() > 1 && secondGroup is not null && Winner.CanFlattenOthers(secondGroup)) {
            AuxiliaryWinners = secondGroup.ToList();
        }
        else {
            AuxiliaryWinners = Array.Empty<TransientUnit>();
        }
    }

    private IEnumerable<TransientUnit> All => AuxiliaryWinners.Prepend(Winner);

    public IEnumerator<TransientUnit> GetEnumerator()
    {
        return AuxiliaryWinners.Prepend(Winner).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
