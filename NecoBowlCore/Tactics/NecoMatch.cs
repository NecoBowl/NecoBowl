using neco_soft.NecoBowlCore.Action;

namespace neco_soft.NecoBowlCore.Tactics;

public class NecoMatch
{
    public readonly NecoPlayerPair Players;
    public NecoPush CurrentPush;

    internal NecoMatch(NecoPlayerPair? players = null, NecoFieldParameters? fieldParams = null)
    {
        players ??= new(new(), new());
        fieldParams ??= new((5, 5));
        
        Players = players;
        CurrentPush = new NecoPush(Players, fieldParams);
    }
}