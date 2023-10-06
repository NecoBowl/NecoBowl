using NecoBowl.Core.Machine;
using NecoBowl.Core.Machine.Reports;

namespace NecoBowl.Core.Reports;

public record Play : BaseReport
{
    private readonly PlayMachine Machine;

    internal Play(PlayMachine machine)
    {
        Machine = machine;
    }

    public IEnumerable<Step> GetSteps()
    {
        while (!Machine.CanEnd) {
            yield return Machine.Step();
        }
    }
}
