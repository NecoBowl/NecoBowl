using System.Collections.Immutable;
using NecoBowl.Core.Tactics;

namespace NecoBowl.Core.Reports;

public record Plan : BaseReport
{
    private readonly IImmutableList<CardPlay> CardPlays;

    public CardPlay? CardPlayAt(Vector2i pos)
    {
        return CardPlays.SingleOrDefault(p => p.Position == pos);
    }

    internal Plan(Sport.Tactics.Plan realPlan)
    {
        CardPlays = realPlan.GetCardPlays().Select(p => p.ToReport()).ToImmutableList();
    }

    public record CardPlay
    {
        public readonly Card Card;
        public readonly Vector2i Position;

        public CardPlay(Vector2i position, Card card)
        {
            Position = position;
            Card = card;
        }
    }
}
