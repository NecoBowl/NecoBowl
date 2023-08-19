using System.Collections.ObjectModel;

using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlCore.Action;

/// <summary>
///     Record container for a unit that is moving to another space.
/// </summary>
public record NecoUnitMovement
{
    public readonly Vector2i NewPos;
    public readonly Vector2i OldPos;

    // Hack to let the end of step calculation see what initiated the movements (push vs manual move).
    internal readonly NecoUnitActionResult? Source;
    internal readonly NecoUnit Unit;

    public NecoUnitMovement(NecoUnit unit, Vector2i newPos, Vector2i oldPos, NecoUnitActionResult? source = null)
    {
        Unit = unit;
        NewPos = newPos;
        OldPos = oldPos;
        Source = source;
    }

    public NecoUnitMovement(NecoUnitMovement other,
                            Vector2i? newPos = null,
                            Vector2i? oldPos = null,
                            NecoUnitActionResult? source = null)
    {
        NewPos = newPos ?? other.NewPos;
        OldPos = oldPos ?? other.OldPos;
        Source = source ?? other.Source;
        Unit = other.Unit;
    }

    public NecoUnitId UnitId => Unit.Id;

    public bool IsChange => NewPos != OldPos;

    public bool IsChangeInSource
        => Source?.StateChange is NecoUnitActionOutcome.UnitTranslated translation
            ? translation.Movement.IsChange
            : IsChange;

    public Vector2i Difference => NewPos - OldPos;

    public AbsoluteDirection AsDirection()
    {
        return Enum.GetValues<AbsoluteDirection>().Single(d => d.ToVector2i() == Difference);
    }
}

internal record UnitMovementPair
{
    public readonly ReadOnlyCollection<NecoUnitMovement> Collection;

    public readonly NecoUnitMovement Movement1, Movement2;
    public readonly ReadOnlyCollection<NecoUnit> Movements;

    public UnitMovementPair(NecoUnitMovement movement1, NecoUnitMovement movement2)
    {
        Movement1 = movement1;
        Movement2 = movement2;

        Movements = new(new[] {
            Movement1.Unit,
            Movement2.Unit
        });
        Collection = new(new[] {
            Movement1,
            Movement2
        });
    }

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
    ///     Finds the single unit that matches a condition. Throws an exception if both items in the pair match the
    ///     condition.
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

    /// <summary>
    ///     Try to find the single unit that matches a condition.
    /// </summary>
    /// <returns>False if neither unit matches the condition or if both match the condition. Otherwise, true.</returns>
    /// <seealso cref="UnitWhereSingle" />
    public bool TryUnitWhereSingle(Func<NecoUnitMovement, bool> predicate,
                                   out NecoUnitMovement? result,
                                   out NecoUnitMovement? other)
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

    public bool TryGetUnitsBy(Func<NecoUnitMovement, bool> predicate1,
                              Func<NecoUnitMovement, bool> predicate2,
                              out NecoUnitMovement? result1,
                              out NecoUnitMovement? result2)
    {
        if (TryUnitWhereSingle(predicate1, out result1, out result2)) {
            if (predicate2(result2!)) {
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

    public bool IsSameUnitsAs(UnitMovementPair other)
    {
        return (Movement1 == other.Movement1 && Movement2 == other.Movement2)
         || (Movement2 == other.Movement1 && Movement1 == other.Movement2);
    }

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
}

internal static class PlayStepperExt
{
    public static IEnumerable<NecoUnitMovement> OrderByCombatPriority(this IEnumerable<NecoUnitMovement> units)
    {
        return units.OrderByDescending(u => u.Unit.Power)
            .ThenBy(u => u.OldPos.Y)
            .ThenBy(u => u.OldPos.X);
    }

    public static IEnumerable<NecoUnitMovement[]> GroupByFriendlyUnitCollisions(
        this IEnumerable<NecoUnitMovement> units)
    {
        return units.GroupBy(u => (u.NewPos, u.Unit.OwnerId))
            .Where(g => g.Count() > 1)
            .Select(g => g.ToArray());
    }

    public static IEnumerable<NecoUnitMovement[]> GroupByCollisions(this IEnumerable<NecoUnitMovement> units)
    {
        return units.GroupBy(u => u.NewPos)
            .Where(g => g.Count() > 1)
            .Select(g => g.ToArray());
    }

    public static IEnumerable<UnitMovementPair> GroupBySpaceSwaps(this IEnumerable<NecoUnitMovement> movements)
    {
        var movementsTemp = movements.ToList();
        var construct = new List<UnitMovementPair>();
        var usedUnits = new List<NecoUnitMovement>();

        foreach (var move in movementsTemp) {
            if (usedUnits.Contains(move)) {
                continue;
            }

            var swap = movementsTemp.FirstOrDefault(m
                                                        => m.NewPos == move.OldPos
                                                     && move.NewPos == m.OldPos
                                                     && move.Unit != m.Unit);
            if (swap is not null) {
                construct.Add(new(move, swap));
                usedUnits.Add(move);
                usedUnits.Add(swap);
            }
        }

        return construct;
    }
}
