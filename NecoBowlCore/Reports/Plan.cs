using System.Collections.Immutable;
using NecoBowl.Core.Sport.Tactics;

namespace NecoBowl.Core.Reports;

public class Plan
{
    public readonly IImmutableList<Sport.Tactics.Plan.CardPlay> CardPlays;

    public Plan(Sport.Tactics.Plan realPlan)
    {
        CardPlays = realPlan.GetCardPlays().ToImmutableList();
    }
}
