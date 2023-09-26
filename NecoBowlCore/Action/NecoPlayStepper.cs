using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using neco_soft.NecoBowlCore.Tags;
using NLog;

namespace neco_soft.NecoBowlCore.Action;

/// <summary>Represents a unit transitioning between spaces.</summary>
public record NecoUnitMovement
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>The position of the unit after the transition.</summary>
    public readonly Vector2i NewPos;

    /// <summary>The position of the unit before the transition.</summary>
    public readonly Vector2i OldPos;

    /// <summary>Optionally specifies a movement from which this one was copied and modified.</summary>
    public readonly NecoUnitMovement? Source;

    /// <summary>The unit being moved.</summary>
    internal readonly NecoUnit Unit;

    public NecoUnitMovement(NecoUnit unit, Vector2i newPos, Vector2i oldPos, NecoUnitMovement? source = null)
    {
        Unit = unit;
        NewPos = newPos;
        OldPos = oldPos;
        Source = source;
    }

    /// <summary>
    /// Creates a copy of another movement, optionally changing some of its fields. <p /> Note that, due to null semantics, you
    /// cannot pass <c>null</c> as an option to <paramref name="source" /> because that will cause it to fallback to the
    /// <c>source</c> of <paramref name="other" />.
    /// </summary>
    public NecoUnitMovement(
        NecoUnitMovement other,
        Vector2i? newPos = null,
        Vector2i? oldPos = null,
        NecoUnitMovement? source = null)
    {
        NewPos = newPos ?? other.NewPos;
        OldPos = oldPos ?? other.OldPos;
        Unit = other.Unit;
        Source = source ?? other.Source;
    }

    public NecoUnitId UnitId => Unit.Id;

    public bool IsChange => NewPos != OldPos;

    public Vector2i Difference => NewPos - OldPos;

    public AbsoluteDirection AsDirection()
    {
        // TODO Normalize
        return Enum.GetValues<AbsoluteDirection>().Single(d => d.ToVector2i() == Difference);
    }

    internal static IEnumerable<UnitMovementPair> GetMovementPairs(IEnumerable<NecoUnitMovement> movementsList)
    {
        var necoUnitMovements = movementsList.ToList();
        return necoUnitMovements.SelectMany(
            m => necoUnitMovements.Where(m2 => m != m2).Select(m2 => new UnitMovementPair(m, m2)));
    }
}

internal record UnitMovementPair
{
    public readonly NecoUnitMovement Movement1, Movement2;

    public UnitMovementPair(NecoUnitMovement movement1, NecoUnitMovement movement2)
    {
        Movement1 = movement1;
        Movement2 = movement2;
    }

    public ReadOnlyCollection<NecoUnitMovement> Collection => new(new[] { Movement1, Movement2 });

    public NecoUnit Unit1 => Movement1.Unit;
    public NecoUnit Unit2 => Movement2.Unit;

    /// <summary>Finds the unit in the pair with the specified tag.</summary>
    /// <param name="tag">The tag to search for.</param>
    /// <param name="other">The unit in the pair that does not have the tag. Null if both units have the tag.</param>
    /// <returns>The first unit in the pair that has the tag, or null if neither unit has it.</returns>
    public NecoUnitMovement? UnitWithTag(NecoUnitTag tag, out NecoUnitMovement? other)
    {
        other = Collection.LastOrDefault(u => !u.Unit.UnitModel.Tags.Contains(tag));
        return Collection.FirstOrDefault(u => u.Unit.UnitModel.Tags.Contains(tag));
    }

    /// <summary>
    /// Finds the single unit that matches a condition. Throws an exception if both items in the pair match the
    /// condition.
    /// </summary>
    /// <param name="predicate">The condition to check for.</param>
    /// <param name="other">The unit of the pair that did not match the condition. Null if neither unit matches.</param>
    /// <returns>The unit in the pair that matches the condition. Null if neither unit matches.</returns>
    public NecoUnitMovement? UnitWhereSingle(Func<NecoUnitMovement, bool> predicate, out NecoUnitMovement? other)
    {
        var movement = Collection.SingleOrDefault(predicate);
        other = movement is not null ? OtherMovement(movement) : null;
        return movement;
    }

    /// <summary>Try to find the single unit that matches a condition.</summary>
    /// <returns>False if neither unit matches the condition or if both match the condition. Otherwise, true.</returns>
    /// <seealso cref="UnitWhereSingle" />
    public bool TryUnitWhereSingle(
        Func<NecoUnitMovement, bool> predicate,
        [NotNullWhen(true)] out NecoUnitMovement? result,
        [NotNullWhen(true)] out NecoUnitMovement? other)
    {
        try {
            result = UnitWhereSingle(predicate, out other);
        }
        catch (InvalidOperationException) {
            result = null;
            other = null;
        }

        return result is not null;
    }

    public bool TryGetUnitsBy(
        Func<NecoUnitMovement, bool> predicate1,
        Func<NecoUnitMovement, bool> predicate2,
        [NotNullWhen(true)] out NecoUnitMovement? result1,
        [NotNullWhen(true)] out NecoUnitMovement? result2)
    {
        if (TryUnitWhereSingle(predicate1, out result1, out result2)) {
            if (predicate2(result2)) {
                return true;
            }

            result2 = null;
        }

        return false;
    }

    public bool UnitsAreEnemies()
    {
        return Movement1.Unit.OwnerId != default && Movement2.Unit.OwnerId != default
            && Movement1.Unit.OwnerId != Movement2.Unit.OwnerId;
    }

    public bool UnitsAreFriendlies()
    {
        return Movement1.Unit.OwnerId != default && Movement2.Unit.OwnerId != default
            && Movement1.Unit.OwnerId == Movement2.Unit.OwnerId;
    }

    public bool IsSameUnitsAs(UnitMovementPair other)
    {
        return (Movement1 == other.Movement1 && Movement2 == other.Movement2)
            || (Movement2 == other.Movement1 && Movement1 == other.Movement2);
    }

    public bool PickupCanOccur(
        [MaybeNullWhen(false)] out NecoUnitMovement carrier,
        [MaybeNullWhen(false)] out NecoUnitMovement item)
    {
        if (UnitWithTag(NecoUnitTag.Carrier, out var itemUnit) is { } carrierUnit) {
            if (itemUnit is not null && itemUnit.Unit.Tags.Contains(NecoUnitTag.Item)) {
                carrier = carrierUnit;
                item = itemUnit;
                return true;
            }
        }

        carrier = null;
        item = null;
        return false;
    }

    /// <summary>Get the transition in the pair that is not the given one.</summary>
    /// <param name="movement">The transition of which to find the pair-mate.</param>
    /// <exception cref="NecoBowlException">The given movement is not in the pair.</exception>
    public NecoUnitMovement OtherMovement(NecoUnitMovement movement)
    {
        if (Movement1 == movement && Movement2 != movement) {
            return Movement2;
        }

        if (Movement2 == movement && Movement1 != movement) {
            return Movement1;
        }

        if (Movement1 != movement && Movement2 != movement) {
            throw new NecoBowlException("movement is not in pair");
        }

        throw new NecoBowlException("both units are the same");
    }

    public bool UnitsCanFight()
    {
        return UnitsAreEnemies();
    }

    public bool IsSpaceSwap()
    {
        return Movement1.NewPos == Movement2.OldPos && Movement1.OldPos == Movement2.NewPos;
    }

    public UnitMovementPair Opposite()
    {
        return new(Movement2, Movement1);
    }
}
