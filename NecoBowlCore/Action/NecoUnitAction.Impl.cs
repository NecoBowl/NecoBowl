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
            var movementDirection = unit.Facing.RotatedBy(Direction);
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

            var (ballPos, ball, _)
                = field.GetAllUnitsWithInventory().SingleOrDefault(tup => tup.Item2.Tags.Contains(NecoUnitTag.TheBall));
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
        private readonly RelativeDirection[] AllowedDirections;

        public ChaseBall(RelativeDirection[] allowedDirections)
        {
            AllowedDirections = allowedDirections;
        }

        protected override NecoUnitActionResult CallResult(NecoUnitId uid, ReadOnlyNecoField field)
        {
            var unit = field.GetUnit(uid, out var pos);
            var (ballPos, ball)
                = field.GetAllUnits().SingleOrDefault(tup => tup.Item2.Tags.Contains(NecoUnitTag.TheBall));
            var result = AllowedDirections.MinBy(dir => (pos + dir.ToVector2i(unit.Facing) - ballPos).LengthSquared);
            return new TranslateUnit(result).CallResult(uid, field);
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
