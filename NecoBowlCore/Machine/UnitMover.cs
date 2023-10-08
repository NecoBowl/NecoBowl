using System.Collections;
using System.Collections.Immutable;
using NecoBowl.Core.Machine.Mutations;
using NecoBowl.Core.Sport.Play;
using NecoBowl.Core.Tags;
using NLog;

namespace NecoBowl.Core.Machine;

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
            MutationReceiver.BufferMutation(new UnitBumps(outOfBounds.Unit.ToReport(), outOfBounds.AsDirection()));
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
                    MutationReceiver.BufferMutation(new UnitBumps(unit.ToReport(), change.Movement.AsDirection()));
                    break;
                }

                case SpaceImmigrantRemovalReason.Removed removal: {
                    // don't put this in MovementsList
                    MovementsList.Remove(change.Movement.Unit);
                    continue;
                }

                case SpaceImmigrantRemovalReason.Combat combat: {
                    foreach (var opponent in combat.Other) {
                        MutationReceiver.BufferMutation(new UnitAttacks(unit.ToReport(), opponent.ToReport()));
                    }

                    break;
                }
            }

            MovementsList[change.Movement.Unit] = change.Movement.WithoutMovement();
        }

        // Transfer buffer over
        MutationReceiver.BufferMutations(transientField.Mutations);
        foreach (var flattening in transientField.FlattenedUnitBuffer) {
            Playfield.FlattenedMovementUnitBuffer.Add(flattening);
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
        Bounce = 0,
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
                        pair.Unit1.ToReport(),
                        pair.Unit2.ToReport()));
            }
            else {
                MutationReceiver.BufferMutation(new UnitBumps(pair.Unit1.ToReport(), pair.Movement1.AsDirection()));
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

            MutationReceiver.BufferMutation(new UnitBumps(pair.Unit1.ToReport(), pair.Movement1.AsDirection()));
            winner = null;
        }
    }

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
            TransientUnit movement, CollisionVictoryReasonLevel level) =>
            new Dictionary<TransientUnit, CollisionVictoryReasonLevel> {
                [movement] = level,
                [unitPair.OtherMovement(movement)] = CollisionVictoryReasonLevel.Bounce,
            };
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

    public readonly IImmutableList<Unit> FlattenedUnitBuffer;
    public readonly IImmutableList<Mutation> Mutations;
    public readonly IImmutableDictionary<Unit, SpaceImmigrantRemoval> ResetReasons;
    public readonly IImmutableDictionary<Vector2i, Space> Spaces;

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
                foreach (var removal in space.Removals) {
                    var movement = removal.Movement.WithoutMovement();
                    if (resetReasons.Keys.Any(u => u == movement.Unit)) {
                        Logger.Warn($"Multiple reset reasons for {removal.Movement.Unit}");
                    }

                    resetReasons[movement.Unit] = removal;

                    // If the item was removed by Removed, we stop before registering it as on-field.
                    if (removal.Reason is SpaceImmigrantRemovalReason.Removed) {
                        continue;
                    }

                    unstableSpaces[movement.OldPos].Add(movement);
                }

                if (space.Winners is { }) {
                    unstableSpaces[pos].Add(space.Winners.Winner);
                    FlattenedUnitBuffer.AddRange(space.Winners.AuxiliaryWinners.Select(u => u.Unit));
                    Mutations.AddRange(space.Winners.FlatteningMutations);
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
    public class Space
    {
        private readonly IReadOnlyList<TransientUnit> Immigrants;
        private readonly ITransientSpaceContentsGetter Playfield;
        private readonly Vector2i Position;
        public readonly ImmutableList<SpaceImmigrantRemoval> Removals;
        public readonly WinnerList? Winners;

        public Space(
            Vector2i position, IEnumerable<TransientUnit> immigrants, ITransientSpaceContentsGetter playfield)
        {
            Immigrants = immigrants.ToList().AsReadOnly();
            Position = position;
            Playfield = playfield;

            if (Immigrants.Any(movement => movement.NewPos != Position)) {
                throw new InvalidOperationException("movement destination differs from space position");
            }

            Removals = GetRemovedImmigrants(out Winners).ToImmutableList();
        }

        public bool HasConflict => Immigrants.Count > 1;

        public IReadOnlyCollection<TransientUnit> GetAllImmigrants()
        {
            return Immigrants;
        }

        /// <summary>
        /// Get all the units that must be removed from this space before it can be turned back into a <see cref="Playfield" />
        /// space.
        /// </summary>
        private IReadOnlyCollection<SpaceImmigrantRemoval> GetRemovedImmigrants(out WinnerList? winners)
        {
            var combats = new Dictionary<TransientUnit, List<TransientUnit>>();
            var bestVictoryLevels =
                new Dictionary<TransientUnit, PlayfieldCollisionResolver.CollisionVictoryReasonLevel>();

            var combinations = PlayfieldCollisionResolver.GetPairCombinations(Immigrants);
            foreach (var pair in combinations) {
                foreach (var movement in pair) {
                    if (movement.Unit.CanAttackOther(pair.OtherMovement(movement).Unit)) {
                        if (!combats.ContainsKey(movement)) {
                            combats[movement] = new();
                        }

                        combats[movement].Add(pair.OtherMovement(movement));
                    }
                }

                foreach (var (movement, level) in PlayfieldCollisionResolver.GetCollisionScores(pair)) {
                    if ((int)bestVictoryLevels.GetValueOrDefault(
                            movement, PlayfieldCollisionResolver.CollisionVictoryReasonLevel.Bounce) >= (int)level) {
                        bestVictoryLevels[movement] = level;
                    }
                }
            }

            winners = null;
            var removals = new List<SpaceImmigrantRemoval>();

            if (combats.Any() || bestVictoryLevels.Any()) {
                winners = bestVictoryLevels.Any()
                    ? new WinnerList(
                        bestVictoryLevels
                            .GroupBy(kv => (int)kv.Value, kv => kv.Key)
                            .OrderBy(g => g.Key))
                    : null;

                foreach (var immigrant in Immigrants) {
                    if (immigrant == Winners?.Winner) {
                        continue;
                    }

                    if (combats.TryGetValue(immigrant, out var targets)) {
                        removals.Add(
                            new(immigrant, new SpaceImmigrantRemovalReason.Combat(targets.Select(m => m.Unit))));
                    }
                    else if (winners?.AuxiliaryWinners.Contains(immigrant) ?? false) {
                        removals.Add(new(immigrant, new SpaceImmigrantRemovalReason.Removed()));
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
        public readonly Unit Remover;

        public Removed(Unit remover)
        {
            Remover = remover;
        }
    }
}

internal interface ITransientSpaceContentsGetter
{
    public IReadOnlyCollection<TransientUnit> GetMovementsFrom(Vector2i pos);
}

internal class WinnerList : IEnumerable<TransientUnit>
{
    public readonly IReadOnlyCollection<TransientUnit> AuxiliaryWinners;
    public readonly ImmutableList<Mutation> FlatteningMutations;

    public readonly TransientUnit Winner;

    public WinnerList(IEnumerable<IGrouping<int, TransientUnit>> collection)
    {
        collection = collection.ToList();
        var flattenings = new List<Mutation>();

        // Validate.
        if (!collection.Any()) {
            throw new ArgumentException("at least one grouping is required for a winner list");
        }

        if (collection.First().Count() != 1) {
            throw new ArgumentException("first winner group can only have one unit");
        }

        Winner = collection.First().Single();

        var secondGroup = collection.Skip(1).FirstOrDefault()?.ToList();
        if (secondGroup is { } && Winner.CanFlattenOthers(secondGroup)) {
            AuxiliaryWinners = secondGroup.ToList();
        }
        else if (secondGroup?.Count == 1) {
            // If the single member of the 2nd place winners can flatten the 1st place winner, we let the 2nd place winner
            // become the 1st place winner and flatten the old 1st place winner into it.
            var alternativeWinner = secondGroup.Single();
            if (alternativeWinner.CanFlattenOthers(new[] { Winner })) {
                AuxiliaryWinners = new[] { Winner };
                Winner = alternativeWinner;
            }
        }

        AuxiliaryWinners ??= Array.Empty<TransientUnit>();

        if (AuxiliaryWinners.Any()) {
            // Confirmed flattening
            flattenings.Add(new UnitPicksUpItem(Winner.UnitId, AuxiliaryWinners.First().UnitId));
        }

        FlatteningMutations = flattenings.ToImmutableList();
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

internal record UnitFlattening
{
    public IEnumerable<Mutation> Mutations;
    public Unit Unit;

    public UnitFlattening(IEnumerable<Mutation> mutations, Unit unit)
    {
        Mutations = mutations;
        Unit = unit;
    }
}
