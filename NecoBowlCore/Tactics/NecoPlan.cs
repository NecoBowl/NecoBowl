using System.Collections.Generic;

using neco_soft.NecoBowlCore.Action;

namespace neco_soft.NecoBowlCore.Tactics;

/// <summary>
/// The cards placed onto the field by a single player. Mutated over the course of the runtime of a <see cref="NecoPush"/>.
/// </summary>
public class NecoPlan
{
    public record class CardPlay(NecoPlayerId Player, NecoCard Card, Vector2i Position);

    // Gradually build up over multiple turns
    private readonly List<CardPlay> CardPlays = new();
    public IReadOnlyList<CardPlay> GetCardPlays() => CardPlays;

    public NecoPlan()
    {
    }

    public void AddCardPlay(CardPlay play)
        => CardPlays.Add(play);

    public void AddCardPlays(IEnumerable<CardPlay> plays)
        => CardPlays.AddRange(plays);
}