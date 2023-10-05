using NecoBowl.Core.Machine;
using NecoBowl.Core.Tags;

namespace NecoBowl.Core.Sport.Play;

public class TranslateUnit : Behavior
{
    private readonly RelativeDirection Direction;

    public TranslateUnit(RelativeDirection direction)
    {
        Direction = direction;
    }

    internal override BehaviorOutcome CallResult(NecoUnitId uid, ReadOnlyPlayfield field)
    {
        var pos = field.GetUnitPosition(uid);
        var unit = field.GetUnit(pos);
        var flippedDirection = Direction.Mirror(
            unit.GetMod<NecoUnitMod.Flip>().EnableX,
            unit.GetMod<NecoUnitMod.Flip>().EnableY);
        var movementDirection = unit.Facing.RotatedBy(flippedDirection);
        var newPos = pos + movementDirection.ToVector2i();

        var transient = new TransientUnit {
            NewPos = newPos,
            OldPos = pos,
            Unit = unit,
        };

        // TODO Collision check here? Or leave it to caller?
        if (!field.IsInBounds(newPos)) {
            return new BehaviorOutcome.Translate(transient, BehaviorOutcome.Kind.Failure);
        }

        return BehaviorOutcome.Success(transient);
    }
}
