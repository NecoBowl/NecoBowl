using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlCore.Action;

public abstract partial class NecoUnitAction
{
    public class TranslateUnit : NecoUnitAction
    {
        private readonly RelativeDirection Direction;

        public TranslateUnit(RelativeDirection direction)
        {
            Direction = direction;
        }

        protected override NecoUnitActionResult CallResult(NecoUnitId uid, ReadOnlyNecoField field)
        {
            var pos = field.GetUnitPosition(uid);
            var unit = field.GetUnit(pos);
            var flippedDirection = Direction.Mirror(
                unit.GetMod<NecoUnitMod.Flip>().EnableX,
                unit.GetMod<NecoUnitMod.Flip>().EnableY);
            var movementDirection = unit.Facing.RotatedBy(flippedDirection);
            var newPos = pos + movementDirection.ToVector2i();

            var outcome = new NecoUnitActionOutcome.UnitTranslated(new(unit, newPos, pos));

            if (!field.IsInBounds(newPos)) {
                return NecoUnitActionResult.Failure($"{unit} could not move {Direction} (out of bounds)", outcome);
            }

            return NecoUnitActionResult.Success(outcome);
        }
    }

    public class TranslateUnitCrabwalk : NecoUnitAction
    {
        protected override NecoUnitActionResult CallResult(NecoUnitId uid, ReadOnlyNecoField field)
        {
            var pos = field.GetUnitPosition(uid);
            var unit = field.GetUnit(pos);

            var (ballPos, ball)
                = field.GetAllUnits(true).SingleOrDefault(tup => tup.Item2.Tags.Contains(NecoUnitTag.TheBall));
            if (ball is null) {
                throw new NecoUnitActionException("no ball found on field");
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

            return NecoUnitActionResult.Success(new NecoUnitActionOutcome.NothingHappened(uid));
        }
    }

    public class ChaseBall : NecoUnitAction
    {
        public const string Option_FallbackDirecttion = "FallbackDirection";
        private readonly RelativeDirection[] AllowedDirections;
        private readonly RelativeDirection FallbackDirection;

        public ChaseBall(RelativeDirection[] allowedDirections, RelativeDirection? fallback = null)
        {
            if (allowedDirections.Length == 0) {
                throw new NecoUnitActionException("no directions provided");
            }

            FallbackDirection = fallback ?? allowedDirections[0];
            AllowedDirections = allowedDirections;
        }

        protected override NecoUnitActionResult CallResult(NecoUnitId uid, ReadOnlyNecoField field)
        {
            var unit = field.GetUnit(uid, out var pos);
            var (ballPos, ball)
                = field.GetAllUnits().SingleOrDefault(tup => tup.Item2.Tags.Contains(NecoUnitTag.TheBall));
            var (minDistanceDirection, minDistanceAfterMove) = AllowedDirections.Select(
                    dir => {
                        var lengthSquared = (pos + dir.ToVector2i(unit.Facing) - ballPos).LengthSquared;
                        return (dir, lengthSquared);
                    })
                .MinBy(tuple => tuple.lengthSquared);
            var originalDistance = (pos - ballPos).LengthSquared;

            var fallbackDirection = (RelativeDirection)(unit.GetMod<NecoUnitMod.OptionValues>()
                .GetValueOrNull<RelativeDirection>(Option_FallbackDirecttion) ?? FallbackDirection);
            var direction = minDistanceAfterMove > originalDistance ? fallbackDirection : minDistanceDirection;
            return new TranslateUnit(direction).CallResult(uid, field);
        }
    }

    public class ApplyMod : NecoUnitAction
    {
        public readonly NecoUnitMod Mod;

        public ApplyMod(NecoUnitMod mod)
        {
            Mod = mod;
        }

        protected override NecoUnitActionResult CallResult(NecoUnitId uid, ReadOnlyNecoField field)
        {
            var unit = field.GetUnit(uid, out var pos);
            return NecoUnitActionResult.Success(new NecoUnitActionOutcome.UnitChanged(Mod));
        }
    }

    public class DoNothing : NecoUnitAction
    {
        protected override NecoUnitActionResult CallResult(NecoUnitId uid, ReadOnlyNecoField field)
        {
            return NecoUnitActionResult.Success(new NecoUnitActionOutcome.NothingHappened(uid));
        }
    }
}
