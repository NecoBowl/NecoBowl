using neco_soft.NecoBowlCore.Tactics;

namespace neco_soft.NecoBowlCore.Input;

public abstract class NecoInput
{
    public NecoPlayerId PlayerId;

    public NecoInput(NecoPlayer player)
    {
        PlayerId = player.Id;
    }

    public sealed class PlaceCard : NecoInput
    {
        public readonly NecoCard Card;
        public readonly Vector2i Position;

        public PlaceCard(NecoPlayer player, NecoCard card, Vector2i position)
            : base(player)
        {
            Card = card;
            Position = position;
        }
    }

    public sealed class SetPlanMod : NecoInput
    {
        public readonly NecoCard Card;
        public readonly string OptionIdentifier;
        public readonly object OptionValue;

        public SetPlanMod(NecoPlayer player, NecoCard card, string optionIdentifier, object optionValue)
            : base(player)
        {
            Card = card;
            OptionIdentifier = optionIdentifier;
            OptionValue = optionValue;
        }
    }
}

public class NecoInputResponse
{
    public enum Kind
    {
        Success,
        Illegal,
        Error
    }

    public readonly Exception? Exception;
    public readonly string? Message;

    public readonly Kind ResponseKind;

    public NecoInputResponse(Kind responseKind, string? message = null, Exception? exception = null)
    {
        ResponseKind = responseKind;
        Message = message;
        Exception = exception;
    }

    public static NecoInputResponse Success()
    {
        return new(Kind.Success);
    }

    public static NecoInputResponse Illegal(string message)
    {
        return new(Kind.Illegal, message);
    }

    public static NecoInputResponse Error(Exception exception)
    {
        return new(Kind.Error, exception: exception);
    }
}

public class NecoInputException : NecoBowlException
{
    public NecoInputException()
    { }

    public NecoInputException(string message) : base(message)
    { }

    public NecoInputException(string message, Exception inner) : base(message, inner)
    { }
}
