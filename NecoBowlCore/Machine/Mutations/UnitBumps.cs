using NecoBowl.Core.Machine;

namespace NecoBowl.Core.Sport.Play;

public class UnitBumps : Mutation
{
    public readonly AbsoluteDirection Direction;

    public UnitBumps(NecoUnitId subject, AbsoluteDirection direction)
        : base(subject)
    {
        Direction = direction;
    }

    public override string Description => $"{Subject} bumps to the {Direction}";
}
