namespace NecoBowl.Core.Machine;

public abstract class BaseBehavior
{
    public BaseBehavior? Next { get; private set; }

    public bool HasNext => Next is { };

    internal BehaviorOutcome Result(NecoUnitId uid, ReadOnlyPlayfield field)
    {
        try {
            return CallResult(uid, field);
        }
        catch (Exception e) {
            return BehaviorOutcome.Error(e);
        }
    }

    internal abstract BehaviorOutcome CallResult(NecoUnitId uid, ReadOnlyPlayfield field);

    public BaseBehavior Chain(BaseBehavior other)
    {
        Next = other;
        return this;
    }
}

public class BehaviorOutcome
{
    public enum Kind
    {
        Success,
        Failure,
        Error,
    }

    public readonly Exception? Exception;

    public readonly string Message;
    public readonly Kind ResultKind;

    public BehaviorOutcome(
        string message,
        Kind resultKind,
        Exception? exception = null)
    {
        Message = message;
        ResultKind = resultKind;
        Exception = exception;
    }

    internal static BehaviorOutcome Success(BaseMutation mutation)
    {
        return new Mutate(mutation);
    }

    internal static BehaviorOutcome Success(TransientUnit movement)
    {
        return new Translate(movement, Kind.Success);
    }

    internal static BehaviorOutcome Failure(string message)
    {
        return new(message, Kind.Failure);
    }

    internal static BehaviorOutcome Error(Exception exception)
    {
        return new(exception.Message, Kind.Error, exception);
    }

    internal class Translate : BehaviorOutcome
    {
        public readonly TransientUnit Movement;

        public Translate(TransientUnit movement, Kind resultKind)
            : base($"{movement.OldPos} -> {movement.NewPos}", resultKind)
        {
            Movement = movement;
        }
    }

    internal class Mutate : BehaviorOutcome
    {
        public readonly BaseMutation Mutation;

        public Mutate(BaseMutation mutate)
            : base(mutate.Description, Kind.Success)
        {
            Mutation = mutate;
        }
    }

    internal class Nothing : BehaviorOutcome
    {
        public Nothing() : base("nothing", Kind.Success) { }
    }
}

public class BehaviorExecutionException : Exception
{
    public BehaviorExecutionException(string message) : base(message) { }
}
