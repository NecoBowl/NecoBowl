using NecoBowl.Core.Machine;

namespace NecoBowl.Core.Reports;

public record Movement : BaseReport
{
    public readonly NecoUnitId UnitId;
    public readonly Vector2i OldPos, NewPos;

    internal Movement(Unit unit, Vector2i oldPos, Vector2i newPos)
    {
        UnitId = unit.Id;
        OldPos = oldPos;
        NewPos = newPos;
    }

    public bool IsChange => OldPos != NewPos;

    internal static Movement From(TransientUnit unit)
    {
        return new(unit.Unit, unit.OldPos, unit.NewPos);
    }

    public override string ToString()
    {
        return $"{OldPos} -> {NewPos}";
    }
}
