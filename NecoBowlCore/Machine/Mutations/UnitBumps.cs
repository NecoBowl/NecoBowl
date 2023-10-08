using NecoBowl.Core.Reports;

namespace NecoBowl.Core.Sport.Play;

public class UnitBumps : Mutation
{
    public readonly AbsoluteDirection Direction;

    internal UnitBumps(Unit subject, AbsoluteDirection direction)
        : base(subject.Id)
    {
        Direction = direction;
    }

    public override string Description => $"{Subject} bumps to the {Direction}";
}
