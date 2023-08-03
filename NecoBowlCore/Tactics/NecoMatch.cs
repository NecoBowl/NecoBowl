using neco_soft.NecoBowlCore.Action;

namespace neco_soft.NecoBowlCore.Tactics;

internal class NecoMatch
{
    public readonly NecoPlayerPair Players;
    public NecoPush CurrentPush;

    internal NecoMatch(NecoPlayerPair? players = null, NecoFieldParameters? fieldParams = null)
    {
        players ??= new(new(), new());
        fieldParams ??= new((7, 10), (3, 4));
        
        Players = players;
        CurrentPush = new NecoPush(Players, fieldParams);
    }
}