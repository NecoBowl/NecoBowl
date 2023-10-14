using NecoBowl.Core.Machine;
using Unit = NecoBowl.Core.Reports.Unit;

namespace NecoBowl.Core.Sport.Play;

public class UnitBumps : BaseMutation
{
    public readonly AbsoluteDirection Direction;

    internal UnitBumps(Unit subject, AbsoluteDirection direction)
        : base(subject.Id)
    {
        Direction = direction;
    }

    public override string Description => $"{Subject} bumps to the {Direction}";

    internal override IEnumerable<BaseMutation> GetResultantMutations(ReadOnlyPlayfield field)
    {
        var unit = field.GetUnit(Subject, out var pos);
        if (!field.IsInBounds(pos + Direction.ToVector2i())) {
            yield break;
        }

        if (field[pos + Direction.ToVector2i()].Unit is not { } otherUnit) {
            yield break;
        }

        if (unit.Inventory.Any()) {
            yield return new UnitHandsOffItem(unit, otherUnit, unit.Inventory.First());
        }
    }
}
