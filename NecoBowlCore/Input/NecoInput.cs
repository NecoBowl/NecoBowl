using neco_soft.NecoBowlCore.Tactics;
using neco_soft.NecoBowlCore.Tags;

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
        public readonly NecoCardOptionValue Mod;

        public SetPlanMod(NecoPlayer player, NecoCard card, NecoCardOptionValue mod)
            : base(player)
        {
            Card = card;
            Mod = mod;
        }
    }
}

public class NecoInputResponse
{
    public static NecoInputResponse Success() => new NecoInputResponse(Kind.Success);
    public static NecoInputResponse Illegal(string message) => new NecoInputResponse(Kind.Illegal, message: message);
    public static NecoInputResponse Error(Exception exception) => new NecoInputResponse(Kind.Error, exception: exception);

    public readonly Kind ResponseKind;
    public readonly string? Message;
    public readonly Exception? Exception;
    
    public NecoInputResponse(Kind responseKind, string? message = null, Exception? exception = null)
    {
        ResponseKind = responseKind;
        Message = message;
        Exception = exception;
    }

    public enum Kind
    {
        Success,
        Illegal,
        Error
    }
}

public class NecoInputException : NecoBowlException
{
    public NecoInputException() { }
    public NecoInputException(string message) : base(message) { }
    public NecoInputException(string message, Exception inner) : base(message, inner) { }
}
