using System.Collections.ObjectModel;

namespace NecoBowl.Core.Machine.Reports;

public class PlayReport
{
    private readonly ReadOnlyCollection<StepReport> Steps;

    internal PlayReport(ReadOnlyCollection<StepReport> steps)
    {
        Steps = steps;
    }

    public IEnumerable<StepReport> GetSteps()
    {
        return Steps;
    }
}
