using System;
using System.Drawing;

using neco_soft.NecoBowlCore.Tags;

using NLog.LayoutRenderers.Wrappers;

namespace neco_soft.NecoBowlCore.Action;

/// <summary>
/// A mutation on the field that would be performed by a unit during a single step of a play.
/// </summary>
public abstract class NecoUnitAction
{
    public NecoUnitActionResult Result(NecoUnitId uid, ReadOnlyNecoField field)
    {
        try {
            return CallResult(uid, field);
        } catch (Exception e) {
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
            if (IsPositionOutOfBounds(newPos, field)) {
                return NecoUnitActionResult.Failure($"{unit} could not move {Direction} (out of bounds)");
            }

            // PUSHER
            if (unit.Tags.Contains(NecoUnitTag.Pusher)
                && field.TryGetUnit(newPos, out var pushReceiver)) {
                var pushReceiverNewPos = newPos + movementDirection.ToVector2i();
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
                
                return NecoUnitActionResult.Success(new NecoUnitActionOutcome.UnitPushedOther(
                        new(unit, newPos, pos),
                        new(pushReceiver!, pushReceiverNewPos, newPos))); 
            }

            return NecoUnitActionResult.Success(new NecoUnitActionOutcome.UnitTranslated(new(unit, newPos, pos)));
        }
        
        private bool IsPositionOutOfBounds(Vector2i pos, ReadOnlyNecoField field)
            => pos.X < 0 || pos.X >= field.GetBounds().X || pos.Y < 0 || pos.Y >= field.GetBounds().Y;
    }

    public class TranslateUnitCrabwalk : NecoUnitAction
    {
        protected override NecoUnitActionResult CallResult(NecoUnitId uid, ReadOnlyNecoField field)
        {
            var pos = field.GetUnitPosition(uid);
            var unit = field.GetUnit(pos);

            var (ballPos, ball) = field.GetAllUnits().SingleOrDefault(tup => tup.Item2.Tags.Contains(NecoUnitTag.TheBall));
            if (ball is null) {
                throw new NecoUnitActionException("no ball found on field");
            }

            bool leftOn = false, rightOn = false;
            float leftDist = float.MaxValue, rightDist = float.MaxValue;
            var checkDir = pos + RelativeDirection.Left.ToVector2i(unit.Facing);
            if (field.IsInBounds(checkDir)) {
                leftDist = (pos - checkDir).LengthSquared;
                leftOn = true;
            }

            checkDir = pos + RelativeDirection.Right.ToVector2i(unit.Facing);
            if (field.IsInBounds(checkDir)) {
                rightDist = (pos - checkDir).LengthSquared;
                rightOn = true;
            }

            if (rightOn && leftDist > rightDist) {
                return new TranslateUnit(RelativeDirection.Right).CallResult(uid, field);
            } else if (leftOn && rightDist > leftDist) {
                return new TranslateUnit(RelativeDirection.Left).CallResult(uid, field);
            } else {
                return NecoUnitActionResult.Success(new NecoUnitActionOutcome.NothingHappened(uid));
            }
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
/// The final result of a unit's action, after it has considered the board state.
///
/// These are consumed by the <see cref="NecoPlayStepper"/>.
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
            => $"{Movement.Unit} moved from {Movement.OldPos} to {Movement.NewPos}";
    }

    public class UnitPushedOther : NecoUnitActionOutcome
    {
        internal readonly NecoUnitMovement Pusher;
        internal readonly NecoUnitMovement Receiver;

        public UnitPushedOther(NecoUnitMovement pusher, NecoUnitMovement receiver)
        {
            Pusher = pusher;
            Receiver = receiver;
        }

        public override string Description
            => $"{Pusher.Unit} pushed {Receiver}";
    }
    
    public class NothingHappened : NecoUnitActionOutcome
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
    public static NecoUnitActionResult Success(NecoUnitActionOutcome change) 
        => new NecoUnitActionResult(change.Description, Kind.Success, stateChange: change);
    public static NecoUnitActionResult Failure(string message) 
        => new NecoUnitActionResult(message, Kind.Failure);
    public static NecoUnitActionResult Error(Exception exception) 
        => new NecoUnitActionResult(exception.Message, Kind.Error, exception: exception);

    public readonly string Message;
    public readonly Kind ResultKind;
    public readonly NecoUnitActionOutcome? StateChange;
    public readonly Exception? Exception;

    public NecoUnitActionResult(string message, Kind resultKind, NecoUnitActionOutcome? stateChange = null, Exception? exception = null)
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