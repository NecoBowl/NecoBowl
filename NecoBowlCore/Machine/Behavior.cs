using NecoBowl.Core.Machine;

namespace NecoBowl.Core.Sport.Play;

public abstract class Behavior
{
    public Behavior? Next { get; private set; }

    public bool HasNext => Next is not null;

    public BehaviorOutcome Result(NecoUnitId uid, ReadOnlyPlayfield field)
    {
        try {
            return CallResult(uid, field);
        }
        catch (Exception e) {
            return BehaviorOutcome.Error(e);
        }
    }

    protected abstract BehaviorOutcome CallResult(NecoUnitId uid, ReadOnlyPlayfield field);

    public Behavior Chain(Behavior other)
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
        Error
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

    internal static BehaviorOutcome Success(Mutation mutation)
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
        public readonly Mutation Mutation;

        public Mutate(Mutation mutate)
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
