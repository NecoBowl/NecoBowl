using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlCore.Action;

public abstract partial class NecoUnitAction
{
    public NecoUnitAction? Next { get; private set; }

    public bool HasNext => Next is not null;

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

    public NecoUnitAction Chain(NecoUnitAction other)
    {
        Next = other;
        return this;
    }
}

/// <summary>
/// The result of a unit's action, after considering the board state (but before collision calculation).
///
/// These are consumed by the <see cref="NecoPlayStepperNew" />.
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

    public class UnitChanged : NecoUnitActionOutcome
    {
        public readonly NecoUnitMod Mod;

        public UnitChanged(NecoUnitMod mod)
        {
            Mod = mod;
        }

        public override string Description => $"Mod {Mod} applied";
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
