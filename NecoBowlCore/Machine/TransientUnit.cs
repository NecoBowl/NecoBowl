using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using NecoBowl.Core.Machine;
using NecoBowl.Core.Tags;
using NLog;

namespace NecoBowl.Core.Sport.Play;

/// <summary>Represents a unit transitioning between spaces.</summary>
internal record TransientUnit
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>The position of the unit after the transition.</summary>
    public required Vector2i NewPos { get; init; }

    /// <summary>The position of the unit before the transition.</summary>
    public required Vector2i OldPos { get; init; }

    /// <summary>The unit being moved.</summary>
    public required Unit Unit { get; init; }

    public NecoUnitId UnitId => Unit.Id;

    public bool IsChange => NewPos != OldPos;

    public Vector2i Difference => NewPos - OldPos;

    /// <returns>A copy of this movement, but with <see cref="NewPos" /> equal to the current value of <see cref="OldPos" />.</returns>
    public TransientUnit WithoutMovement()
    {
        return new() {
            NewPos = OldPos,
            OldPos = OldPos,
            Unit = Unit
        };
    }

    public AbsoluteDirection AsDirection()
    {
        // TODO Normalize
        var difference = Difference;
        return Enum.GetValues<AbsoluteDirection>().Single(d => d.ToVector2i() == difference);
    }

    public bool CanFlattenOthers(IEnumerable<TransientUnit> others)
    {
        others = others.ToList();
        return others.Count() switch {
            0 => true,
            1 => Unit.CanPickUp(others.Single().Unit),
            _ => false
        };
    }

    public override string ToString()
    {
        return $"[{Unit}: {OldPos} -> {NewPos}]";
    }
}

internal record UnitMovementPair : IEnumerable<TransientUnit>
{
    public readonly TransientUnit Movement1, Movement2;

    public UnitMovementPair(TransientUnit movement1, TransientUnit movement2)
    {
        Movement1 = movement1;
        Movement2 = movement2;
    }

    public ReadOnlyCollection<TransientUnit> Collection => new(new[] { Movement1, Movement2 });

    public Unit Unit1 => Movement1.Unit;
    public Unit Unit2 => Movement2.Unit;

    public IEnumerator<TransientUnit> GetEnumerator()
    {
        return Collection.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>Finds the unit in the pair with the specified tag.</summary>
    /// <param name="tag">The tag to search for.</param>
    /// <param name="other">The unit in the pair that does not have the tag. Null if both units have the tag.</param>
    /// <returns>The first unit in the pair that has the tag, or null if neither unit has it.</returns>
    public TransientUnit? UnitWithTag(NecoUnitTag tag, out TransientUnit? other)
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
    public TransientUnit? UnitWhereSingle(Func<TransientUnit, bool> predicate, out TransientUnit? other)
    {
        var movement = Collection.SingleOrDefault(predicate);
        other = movement is not null ? OtherMovement(movement) : null;
        return movement;
    }

    /// <summary>Try to find the single unit that matches a condition.</summary>
    /// <returns>False if neither unit matches the condition or if both match the condition. Otherwise, true.</returns>
    /// <seealso cref="UnitWhereSingle" />
    public bool TryUnitWhereSingle(
        Func<TransientUnit, bool> predicate,
        [NotNullWhen(true)] out TransientUnit? result,
        [NotNullWhen(true)] out TransientUnit? other)
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
        Func<TransientUnit, bool> predicate1,
        Func<TransientUnit, bool> predicate2,
        [NotNullWhen(true)] out TransientUnit? result1,
        [NotNullWhen(true)] out TransientUnit? result2)
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
        [MaybeNullWhen(false)] out TransientUnit carrier,
        [MaybeNullWhen(false)] out TransientUnit item)
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
    public TransientUnit OtherMovement(TransientUnit movement)
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
