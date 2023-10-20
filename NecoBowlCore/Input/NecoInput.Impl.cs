using System.Text.Json.Serialization;
using NecoBowl.Core.Sport.Tactics;
using NecoBowl.Core.Tactics;

namespace NecoBowl.Core.Input;

public abstract partial class NecoInput
{
    public sealed class PlaceCard : NecoInput
    {
        public Card Card { get; private set; }
        public Vector2i Position { get; private set; }

        public PlaceCard(NecoPlayerId player, Card card, Vector2i position, bool dryRun = false)
            : base(player, dryRun)
        {
            Card = card;
            Position = position;
        }
    }

    public sealed class SetPlanMod : NecoInput
    {
        public Card Card { get; private set; }
        public string OptionIdentifier { get; private set; }
        public object OptionValue { get; private set; }

        public SetPlanMod(NecoPlayerId player, Card card, string optionIdentifier, object optionValue, bool dryRun = false)
            : base(player, dryRun)
        {
            Card = card;
            OptionIdentifier = optionIdentifier;
            OptionValue = optionValue;
        }
    }

    public sealed class RequestEndTurn : NecoInput
    {
        public RequestEndTurn(NecoPlayerId player, bool dryRun)
            : base(player, dryRun)
        {
        }
    }
}
