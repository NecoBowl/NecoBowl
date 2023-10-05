using NecoBowl.Core.Sport.Tactics;

namespace NecoBowl.Core.Input;

public abstract partial class NecoInput
{
    public sealed class PlaceCard : NecoInput
    {
        public readonly Card Card;
        public readonly Vector2i Position;

        public PlaceCard(Player player, Card card, Vector2i position)
            : base(player)
        {
            Card = card;
            Position = position;
        }
    }

    public sealed class SetPlanMod : NecoInput
    {
        public readonly Card Card;
        public readonly string OptionIdentifier;
        public readonly object OptionValue;

        public SetPlanMod(Player player, Card card, string optionIdentifier, object optionValue)
            : base(player)
        {
            Card = card;
            OptionIdentifier = optionIdentifier;
            OptionValue = optionValue;
        }
    }

    public sealed class RequestEndPlay : NecoInput
    {
        public RequestEndPlay(Player player)
            : base(player)
        {
        }
    }
}
