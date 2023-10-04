using NecoBowl.Core.Sport.Play;

namespace NecoBowl.Core.Sport.Tactics;

internal class NecoMatch
{
    public readonly NecoPlayerPair Players;
    public NecoPush CurrentPush;

    internal NecoMatch(NecoPlayerPair? players = null, NecoFieldParameters? fieldParams = null)
    {
        players ??= new(new(), new());
        fieldParams ??= new((7, 11), (3, 4));

        Players = players;
        CurrentPush = new(Players, fieldParams);
    }
}
