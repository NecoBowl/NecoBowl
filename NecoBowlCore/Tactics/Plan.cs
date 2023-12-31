using NecoBowl.Core.Tactics;

namespace NecoBowl.Core.Sport.Tactics;

/// <summary>
/// The cards placed onto the field by a single player. Mutated over the course of the runtime of a
/// <see cref="Push" /> .
/// </summary>
internal class Plan
{
    // Gradually build up over multiple turns
    private readonly List<CardPlay> CardPlays = new();

    public IReadOnlyList<CardPlay> GetCardPlays()
    {
        return CardPlays;
    }

    public void AddCardPlay(CardPlay play)
    {
        CardPlays.Add(play);
    }

    public void AddCardPlays(IEnumerable<CardPlay> plays)
    {
        CardPlays.AddRange(plays);
    }

    public record class CardPlay(NecoPlayerId Player, Card Card, Vector2i Position)
    {
        internal Reports.Plan.CardPlay ToReport()
        {
            return new(Player, Position, Card);
        }
    }
}
