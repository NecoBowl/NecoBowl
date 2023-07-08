using neco_soft.NecoBowlCore.Action;

namespace neco_soft.NecoBowlCore.Tactics;

/// <summary>
/// The cards placed onto the field by a single player.
/// </summary>
public class NecoPlan
{
    public readonly record struct CardPlay(NecoCard Card, Vector2i Position);

    // Gradually build up over multiple turns
    private readonly List<CardPlay> CardPlays = new();
    public IReadOnlyList<CardPlay> GetCardPlays() => CardPlays;

    public NecoPlan()
    {
    }

    public void AddCardPlay(NecoCard card, Vector2i position)
    {
        CardPlays.Add(new (card, position));   
    }
    
    public void AddCardPlay(CardPlay play)
    {
        CardPlays.Add(play);   
    }
}