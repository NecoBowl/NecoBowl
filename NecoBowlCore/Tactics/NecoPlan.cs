using System.Drawing;

namespace neco_soft.NecoBowlCore.Tactics;

public readonly record struct NecoPlanId(Guid Value);

/// <summary>
/// Represents all of the cards placed onto the field by a single player.
/// </summary>
public class NecoPlan
{
    public readonly record struct CardPlay(NecoCard Card, Point Position);

    public readonly NecoPlanId Id = new();
    
    // Gradually build up over multiple turns
    private readonly List<CardPlay> CardPlays = new();
    public IReadOnlyList<CardPlay> GetCardPlays() => CardPlays;

    public void AddCardPlay(NecoCard card, Point position)
        => CardPlays.Add(new (card, position));
}