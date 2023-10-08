using NecoBowl.Core.Sport.Tactics;

namespace NecoBowl.Core.Machine.Reports;

public class Space
{
    public readonly Core.Reports.Unit? Unit;
    public NecoPlayerRole Role;

    public Space(Core.Reports.Unit? unit)
    {
        Unit = unit;
    }
}
