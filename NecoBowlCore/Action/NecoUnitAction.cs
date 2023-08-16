using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlCore.Action;

public abstract class NecoUnitAction
{
    public NecoUnitActionResult Result(NecoUnitId uid, ReadOnlyNecoField field)
    {
        try {
            return CallResult(uid, field);
        }
        catch (Exception e) {
            return NecoUnitActionResult.Error(e);
        }
    }

    protected abstract NecoUnitActionResult CallResult(NecoUnitId uid, ReadOnlyNecoField field);

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

            var (ballPos, ball)
                = field.GetAllUnits().SingleOrDefault(tup => tup.Item2.Tags.Contains(NecoUnitTag.TheBall));
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

    public class DoNothing : NecoUnitAction
    {
        protected override NecoUnitActionResult CallResult(NecoUnitId uid, ReadOnlyNecoField field)
        {
            return NecoUnitActionResult.Success(new NecoUnitActionOutcome.NothingHappened(uid));
        }
    }
}

/// <summary>
///     The final result of a unit's action, after it has considered the board state.
///     These are consumed by the <see cref="NecoPlayStepper" />.
/// </summary>
public abstract class NecoUnitActionOutcome
{
    public abstract string Description { get; }

    public class UnitTranslated : NecoUnitActionOutcome
    {
        internal readonly NecoUnitMovement Movement;

        public UnitTranslated(NecoUnitMovement movement)
        {
            Movement = movement;
        }

        public Vector2i Difference => Movement.NewPos - Movement.OldPos;

        public override string Description
            => $"{Movement.Unit} moves from {Movement.OldPos} to {Movement.NewPos}";
    }

    public class NothingHappened : NecoUnitActionOutcome
    {
        public readonly NecoUnitId UnitId;

        public NothingHappened(NecoUnitId unitId)
        {
            UnitId = unitId;
        }

        public override string Description
            => $"{UnitId} does nothing";
    }
}

public class NecoUnitActionResult
{
    public enum Kind
    {
        Success,
        Failure,
        Error
    }

    public readonly Exception? Exception;

    public readonly string Message;
    public readonly Kind ResultKind;
    public readonly NecoUnitActionOutcome? StateChange;

    public NecoUnitActionResult(string message,
                                Kind resultKind,
                                NecoUnitActionOutcome? stateChange = null,
                                Exception? exception = null)
    {
        Message = message;
        ResultKind = resultKind;
        StateChange = stateChange;
        Exception = exception;
    }

    public static NecoUnitActionResult Success(NecoUnitActionOutcome change)
    {
        return new(change.Description, Kind.Success, change);
    }

    public static NecoUnitActionResult Failure(string message, NecoUnitActionOutcome attemptedChange)
    {
        return new(message, Kind.Failure, attemptedChange);
    }

    public static NecoUnitActionResult Error(Exception exception)
    {
        return new(exception.Message, Kind.Error, exception: exception);
    }
}
