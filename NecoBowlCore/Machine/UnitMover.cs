using System.Collections;
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
        foreach (var (unit, change) in transientField.GetRejections()) {
            // Create combat / bump events 
            switch (change.Reason) {
                case SpaceImmigrantRemovalReason.Superseded superseded: {
                    MutationReceiver.BufferMutation(new UnitBumps(unit.ToReport(), change.Movement.AsDirection()));
                    break;
                }

                case SpaceImmigrantRemovalReason.PickedUp pickedUp: {
                    Playfield.FlattenedMovementUnitBuffer.Add(unit);
                    MutationReceiver.BufferMutation(new UnitPicksUpItem(pickedUp.Remover, unit));
                    break;
                }

                case SpaceImmigrantRemovalReason.Combat combat: {
                    foreach (var opponent in combat.Other) {
                        MutationReceiver.BufferMutation(new UnitAttacks(unit.ToReport(), opponent.ToReport(), change.Movement.NewPos));
                    }

                    break;
                }
            }

            if (change.Reason.IsRemoval) {
                // This unit is being removed from the field.
                MovementsList.Remove(unit);
            }
            else {
                // This unit is being put back to its original space.
                MovementsList[change.Movement.Unit] = change.Movement.WithoutMovement();
            }
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

internal static class PlayfieldCollisionResolver
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
    public virtual bool IsRemoval { get; } = false;

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

    public sealed class PickedUp : SpaceImmigrantRemovalReason
    {
        public readonly Unit Remover;

        public PickedUp(Unit remover)
        {
            Remover = remover;
        }

        public override bool IsRemoval => true;
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
