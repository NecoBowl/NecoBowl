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
                    MovementsList.Remove(unit);
                    Playfield.FlattenedMovementUnitBuffer.Add(unit);
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

    public static IDictionary<TransientUnit, CollisionVictoryReasonLevel> GetCollisionScores(
        UnitMovementPair unitPair)
    {
        if (unitPair.Collection.DistinctBy(u => u.NewPos).Count() > 1
            && !unitPair.IsSpaceSwap()) {
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

    public readonly IImmutableList<Mutation> Mutations;
    public readonly IImmutableDictionary<Unit, SpaceImmigrantRemoval> ResetReasons;
    public readonly IImmutableDictionary<Vector2i, Space> Spaces;

    public TransientPlayfield(ReadOnlyPlayfield field, IEnumerable<TransientUnit> movementsEnumerable)
    {
        var movements = movementsEnumerable.ToList();
        var spaces = new Dictionary<Vector2i, Space>();
        var resetReasons = new Dictionary<Unit, SpaceImmigrantRemoval>();
        var mutations = new List<Mutation>();

        // Set up transient space array
        for (var i = 0; i < field.GetBounds().X; i++) {
            for (var j = 0; j < field.GetBounds().Y; j++) {
                var pos = new Vector2i(i, j);
                var movementsToHere = movements.Where(m => m.NewPos == pos);
                var movementsFromHere = movements.SingleOrDefault(m => m.OldPos == pos && m.IsChange);
                spaces[pos] = new(pos, movementsToHere, movementsFromHere);
            }
        }

        // Now we begin the space reassignment algorithm.
        // It repeats until every space has no conflict.
        while (spaces.Values.Any(space => space.HasConflict)) {
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

                    // Attempt to place the unit on the field.
                    unstableSpaces[movement.OldPos].Add(movement);
                }

                if (space.Winners is { }) {
                    unstableSpaces[pos].Add(space.Winners.Winner);
                    mutations.AddRange(space.Winners.FlatteningMutations);
                }
            }

            var unhandledEmigrants =
                movements.Where(
                    m => !unstableSpaces.Values.SelectMany(l => l, (l, u) => u).Contains(m) &&
                        !resetReasons.ContainsKey(m.Unit));
            spaces = unstableSpaces.ToDictionary(
                kv => kv.Key,
                kv => new Space(
                    kv.Key, unstableSpaces[kv.Key],
                    unhandledEmigrants.SingleOrDefault(m => m.OldPos == kv.Key && m.IsChange)));
        }

        // User can access the results via Spaces.
        Spaces = spaces.ToImmutableDictionary();
        ResetReasons = resetReasons.ToImmutableDictionary();
        Mutations = mutations.ToImmutableList();
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
    /// <p>
    /// A space during movement calculation. Stores multiple "immigrants", which are the units attempting to move onto this
    /// space. Upon creation, <see cref="Removals" /> is populated with a list of removal objects that denote the reason that
    /// unit cannot win the space.
    /// </p>
    /// </summary>
    public class Space
    {
        private readonly TransientUnit? Emigrant;
        private readonly IReadOnlyList<TransientUnit> Immigrants;
        private readonly Vector2i Position;
        public readonly ImmutableList<SpaceImmigrantRemoval> Removals;
        public readonly WinnerList? Winners;


        public Space(Vector2i position, IEnumerable<TransientUnit> immigrants, TransientUnit? emigrant)
        {
            Immigrants = immigrants.ToList().AsReadOnly();
            Emigrant = emigrant;
            Position = position;

            if (Immigrants.Any(movement => movement.NewPos != Position)) {
                throw new InvalidOperationException("movement destination differs from space position");
            }

            if (Emigrant is { }) {
                if (Emigrant.OldPos != Position) {
                    throw new InvalidOperationException("emigrant source position differs from given position");
                }

                if (!Emigrant.IsChange) {
                    throw new ArgumentException("emigrant must be moving");
                }
            }

            Removals = GetRemovedImmigrants(out Winners).ToImmutableList();
        }

        public bool HasConflict => Immigrants.Count > 1 || Emigrant is { };

        public IReadOnlyCollection<TransientUnit> GetAllImmigrants()
        {
            return Immigrants;
        }

        /// <summary>
        /// Get all the units that must be removed from this space before it can be turned back into a <see cref="Playfield" />
        /// space.
        /// </summary>
        private IReadOnlyCollection<SpaceImmigrantRemoval> GetRemovedImmigrants(
            out WinnerList? winners)
        {
            var immigrants = Immigrants.ToList();
            var combats = new Dictionary<Unit, List<TransientUnit>>();
            var bestVictoryLevels =
                new Dictionary<TransientUnit, PlayfieldCollisionResolver.CollisionVictoryReasonLevel>();
            TransientUnit? emigrantSwapper = null;

            if (Emigrant is { }) {
                // Perform space swap, and treat the emigrant as a stationary unit if it is placed back here as a reuslt
                var swapper = immigrants.SingleOrDefault(m => new UnitMovementPair(m, Emigrant).IsSpaceSwap());
                if (swapper is { }) {
                    emigrantSwapper = swapper;
                    var spaceSwapPair = new UnitMovementPair(Emigrant, swapper);
                    if (swapper.Unit.CanAttackOther(Emigrant.Unit)) {
                        combats[swapper.Unit] = new() { Emigrant };
                    }
                    else {
                        immigrants.Add(Emigrant.WithoutMovement());
                        bestVictoryLevels[Emigrant.WithoutMovement()] =
                            PlayfieldCollisionResolver.CollisionVictoryReasonLevel.Bounce;
                    }
                }
            }

            // Iterate through all possible unit pairs and add combat or space victory entries.
            var combinations = PlayfieldCollisionResolver.GetPairCombinations(immigrants).ToList();

            foreach (var pair in combinations) {
                foreach (var movement in pair) {
                    if (movement.Unit.CanAttackOther(pair.OtherMovement(movement).Unit)) {
                        if (!combats.ContainsKey(movement.Unit)) {
                            combats[movement.Unit] = new();
                        }

                        combats[movement.Unit].Add(pair.OtherMovement(movement));
                    }
                }

                foreach (var (movement, level) in PlayfieldCollisionResolver.GetCollisionScores(pair)) {
                    if (movement.NewPos != Position) {
                        continue;
                    }

                    if ((int)bestVictoryLevels.GetValueOrDefault(
                            movement, PlayfieldCollisionResolver.CollisionVictoryReasonLevel.Bounce) >= (int)level) {
                        bestVictoryLevels[movement] = level;
                    }
                }
            }

            if (combinations.Count < 1 && Immigrants.Count > 0 && emigrantSwapper is null) {
                // There is only one immigrant and it's not in a space swap
                bestVictoryLevels[Immigrants.Single()] =
                    PlayfieldCollisionResolver.CollisionVictoryReasonLevel.Stationary;
            }

            winners = null;
            var removals = new List<SpaceImmigrantRemoval>();

            if (!combats.Any() && !bestVictoryLevels.Any()) {
                return removals;
            }

            winners = bestVictoryLevels.Any()
                ? new WinnerList(
                    bestVictoryLevels
                        .GroupBy(kv => (int)kv.Value, kv => kv.Key)
                        .OrderBy(g => g.Key))
                : null;

            // Create a removal reason for each unit not on the final space
            foreach (var immigrant in immigrants) {
                if (immigrant == Winners?.Winner && immigrant != emigrantSwapper) {
                    continue;
                }

                if (combats.TryGetValue(immigrant.Unit, out var targets)) {
                    removals.Add(new(immigrant, new SpaceImmigrantRemovalReason.Combat(targets.Select(m => m.Unit))));
                }
                else if (winners?.AuxiliaryWinners.Contains(immigrant) ?? false) {
                    removals.Add(new(immigrant, new SpaceImmigrantRemovalReason.Removed(winners.Winner.Unit)));
                }
                else {
                    removals.Add(new(immigrant, new SpaceImmigrantRemovalReason.Superseded()));
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

        var firstLevel = collection.First().ToList();
        if (firstLevel.Count != 1) {
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
