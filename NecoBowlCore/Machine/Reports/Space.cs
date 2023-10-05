using NecoBowl.Core.Sport.Tactics;

namespace NecoBowl.Core.Machine.Reports;

public class Space
{
    public readonly Unit? Unit;
    public NecoPlayerRole Role;

    public Space(Unit? unit)
    {
        Unit = unit;
    }
}
