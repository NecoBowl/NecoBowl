using NecoBowl.Core.Machine;
using NecoBowl.Core.Tags;

namespace NecoBowl.Core.Sport.Play;

public class TranslateUnitCrabwalk : BaseBehavior
{
    internal override BehaviorOutcome CallResult(NecoUnitId uid, ReadOnlyPlayfield field)
    {
        var pos = field.GetUnitPosition(uid);
        var unit = field.GetUnit(pos);

        var (ballPos, ball)
            = field.GetAllUnits(true).SingleOrDefault(tup => tup.Item2.Tags.Contains(NecoUnitTag.TheBall));
        if (ball is null) {
            throw new BehaviorExecutionException("no ball found on field");
        }

        bool leftOn = false, rightOn = false;
        float leftDist = float.MaxValue, rightDist = float.MaxValue;
        var checkDir = pos + RelativeDirection.Left.ToVector2i(unit.Facing);
        if (field.IsInBounds(checkDir)) {
            leftDist = (checkDir - ballPos).LengthSquared;
            leftOn = true;
        }

        checkDir = pos + RelativeDirection.Right.ToVector2i(unit.Facing);
        if (field.IsInBounds(checkDir)) {
            rightDist = (checkDir - ballPos).LengthSquared;
            rightOn = true;
        }

        if (rightOn && leftDist > rightDist) {
            return new TranslateUnit(RelativeDirection.Right).CallResult(uid, field);
        }

        if (leftOn && rightDist > leftDist) {
            return new TranslateUnit(RelativeDirection.Left).CallResult(uid, field);
        }

        return new BehaviorOutcome.Nothing();
    }
}
