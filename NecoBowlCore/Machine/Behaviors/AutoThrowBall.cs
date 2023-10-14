using NecoBowl.Core.Sport.Play;
using TupleSplatter;

namespace NecoBowl.Core.Machine.Behaviors;

/// <summary>Tries to throw the ball to the farthest-away friendly unit.</summary>
public class AutoThrowBall : BaseBehavior
{
    internal override BehaviorOutcome CallResult(NecoUnitId uid, ReadOnlyPlayfield field)
    {
        var subjectUnit = field.GetUnit(uid, out var unitPos);

        if (subjectUnit.HandoffItem() is not { } itemUnit) {
            return BehaviorOutcome.Failure("no item to throw");
        }

        var search = field.GetAllUnits()
            .Where(t => t.Splat((pos, unit) => unit.Id != uid && unit.OwnerId == subjectUnit.OwnerId))
            .OrderByDescending(t => t.Splat((pos, unit) => (unitPos - pos).LengthSquared))
            .ToList();

        if (!search.Any()) {
            return BehaviorOutcome.Failure("no friendly units");
        }

        var (resultUnitPos, _) = search.FirstOrDefault();

        return BehaviorOutcome.Success(new UnitThrowsItem(subjectUnit.Id, itemUnit.Id, resultUnitPos));
    }
}
