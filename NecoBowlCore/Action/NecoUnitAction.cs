using System.Drawing;

using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlCore.Action;

public abstract class NecoUnitAction
{
    public abstract NecoUnitActionResult Result(NecoUnitId uid, NecoField field);

    public class TranslateUnit : NecoUnitAction
    {
        private readonly AbsoluteDirection Direction;

        public TranslateUnit(AbsoluteDirection direction)
        {
            Direction = direction;
        }

        public override NecoUnitActionResult Result(NecoUnitId uid, NecoField field)
        { 
            var pos = field.GetUnitPosition(uid);
            var unit = field.GetUnit(pos);
            var newPos = pos + Direction.ToVector2i();
            if (IsPositionOutOfBounds(newPos, field)) {
                return NecoUnitActionResult.Failure($"{unit} could not move {Direction} (out of bounds)");
            }

            // PUSHER
            if (unit.Tags.Contains(NecoUnitTag.Pusher)
                && field.TryGetUnit(newPos, out var pushReceiver)) {
                var pushReceiverNewPos = newPos + Direction.ToVector2i();
                var pushReceiverMovement = new NecoUnitMovement(pushReceiver!, pushReceiverNewPos, newPos); 

                if (IsPositionOutOfBounds(pushReceiverNewPos, field)) {
                    return NecoUnitActionResult.Failure($"{pushReceiver} could not be pushed to {pushReceiverNewPos} (out of bounds)");
                }

                if (field.TryGetUnit(pushReceiverNewPos, out var pushBlocker)) {
                    var pushBlockerMovement = new NecoUnitMovement(pushBlocker!, pushReceiverNewPos, pushReceiverNewPos);
                    if (new UnitPair(pushReceiverMovement, pushBlockerMovement).UnitsAreEnemies()) {
                        // Cannot push another unit into an enemy
                        return NecoUnitActionResult.Failure($"cannot push ({pushReceiver} would collide with {pushBlocker})");
                    }
                }
                
                return NecoUnitActionResult.Success(new NecoFieldStateChange.UnitPushedOther(
                        new(unit, newPos, pos),
                        new(pushReceiver!, pushReceiverNewPos, newPos))); 
            }

            return NecoUnitActionResult.Success(new NecoFieldStateChange.UnitTranslated(new(unit, newPos, pos)));
        }

        private bool IsPositionOutOfBounds(Vector2i pos, NecoField field)
            => pos.X < 0 || pos.X >= field.GetBounds().x || pos.Y < 0 || pos.Y >= field.GetBounds().y;
    }

    public class DoNothing : NecoUnitAction
    {
        public override NecoUnitActionResult Result(NecoUnitId uid, NecoField field)
        {
            return NecoUnitActionResult.Success(new NecoFieldStateChange.NothingHappened(uid));
        }
    }
}

public abstract class NecoFieldStateChange
{
    public abstract string Description { get; }

    public class UnitTranslated : NecoFieldStateChange
    {
        public readonly NecoUnitMovement Movement;

        public UnitTranslated(NecoUnitMovement movement)
        {
            Movement = movement;
        }

        public Vector2i Difference => Movement.NewPos - Movement.OldPos;

        public override string Description
            => $"{Movement.Unit} moved from {Movement.OldPos} to {Movement.NewPos}";
    }

    public class UnitPushedOther : NecoFieldStateChange
    {
        public readonly NecoUnitMovement Pusher;
        public readonly NecoUnitMovement Receiver;

        public UnitPushedOther(NecoUnitMovement pusher, NecoUnitMovement receiver)
        {
            Pusher = pusher;
            Receiver = receiver;
        }

        public override string Description { get; }
    }
    
    public class NothingHappened : NecoFieldStateChange
    {
        public readonly NecoUnitId UnitId;

        public NothingHappened(NecoUnitId unitId)
        {
            UnitId = unitId;
        }

        public override string Description
            => $"{UnitId} did nothing";
    }
}

public class NecoUnitActionResult
{
    public static NecoUnitActionResult Success(NecoFieldStateChange change) 
        => new NecoUnitActionResult(change.Description, Kind.Success, stateChange: change);
    public static NecoUnitActionResult Failure(string message) 
        => new NecoUnitActionResult(message, Kind.Failure);
    public static NecoUnitActionResult Error(Exception exception) 
        => new NecoUnitActionResult(exception.Message, Kind.Error, exception: exception);

    public readonly string Message;
    public readonly Kind ResultKind;
    public readonly NecoFieldStateChange? StateChange;
    public readonly Exception? Exception;

    public NecoUnitActionResult(string message, Kind resultKind, NecoFieldStateChange? stateChange = null, Exception? exception = null)
    {
        Message = message;
        ResultKind = resultKind;
        StateChange = stateChange;
        Exception = exception;
    }

    public enum Kind
    {
        Success,
        Failure,
        Error
    }
}