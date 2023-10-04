using NecoBowl.Core.Sport.Tactics;

namespace NecoBowl.Core.Input;

public abstract partial class NecoInput
{
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

    public sealed class RequestEndPlay : NecoInput
    {
        public RequestEndPlay(NecoPlayer player)
            : base(player)
        {
        }
    }
}
