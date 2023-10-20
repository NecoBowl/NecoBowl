using System.Collections.Immutable;
using NecoBowl.Core.Sport.Tactics;
using NecoBowl.Core.Tactics;

namespace NecoBowl.Core.Reports;

public record Plan : BaseReport
{
    private readonly IImmutableList<CardPlay> CardPlays;

    public CardPlay? CardPlayAt(Vector2i pos)
    {
        return CardPlays.SingleOrDefault(p => p.Position == pos);
    }

    public IEnumerable<CardPlay> GetCardPlays()
    {
        return CardPlays;
    }

    internal Plan(IEnumerable<CardPlay> plays)
    {
        CardPlays = plays.ToImmutableList();
    }

    public record CardPlay
    {
        public readonly NecoPlayerId PlayerId;
        public readonly Card Card;
        public readonly Vector2i Position;

        public CardPlay(NecoPlayerId playerId, Vector2i position, Card card)
        {
            PlayerId = playerId;
            Position = position;
            Card = card;
        }

        internal CardPlay(Sport.Tactics.Plan.CardPlay realPlay)
        {
            PlayerId = realPlay.Player;
            Position = realPlay.Position;
            Card = realPlay.Card;
        }
    }
}
