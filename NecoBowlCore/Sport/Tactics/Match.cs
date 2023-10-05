using NecoBowl.Core.Machine;

namespace NecoBowl.Core.Sport.Tactics;

internal class Match
{
    public readonly NecoPlayerPair Players;
    public Push CurrentPush;

    internal Match(NecoPlayerPair? players = null, NecoFieldParameters? fieldParams = null)
    {
        players ??= new(new(), new());
        fieldParams ??= new((7, 11), (3, 4));

        Players = players;
        CurrentPush = new(Players, fieldParams);
    }
}
