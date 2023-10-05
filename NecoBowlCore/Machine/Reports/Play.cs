using System.Collections.ObjectModel;

namespace NecoBowl.Core.Machine.Reports;

public record Play : BaseReport
{
    private readonly ReadOnlyCollection<Step> Steps;

    internal Play(ReadOnlyCollection<Step> steps)
    {
        Steps = steps;
    }

    public IEnumerable<Step> GetSteps()
    {
        return Steps;
    }
}
