namespace neco_soft.NecoBowlCore.Tactics;

public class NecoMatch
{
    public NecoPlayerPair Players;
    public NecoPush CurrentPush;

    public NecoMatch(NecoPlayerPair players)
    {
        Players = players;
    }
}