using System.Collections.Immutable;
using NecoBowl.Core.Sport.Tactics;

namespace NecoBowl.Core.Reports;

public record PlayerTurn : BaseReport
{
    public NecoPlayerId Id;
    public ImmutableList<Plan.CardPlay> CardPlays;

    public PlayerTurn(NecoPlayerId id, IEnumerable<Plan.CardPlay> cardPlays)
    {
        Id = id;
        CardPlays = cardPlays.ToImmutableList();
    }
}
