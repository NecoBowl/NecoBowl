using NecoBowl.Core.Sport.Play;

namespace NecoBowl.Core.Machine.Reports;

public record Movement : BaseReport
{
    public readonly Vector2i OldPos, NewPos;

    public Movement(Vector2i oldPos, Vector2i newPos)
    {
        OldPos = oldPos;
        NewPos = newPos;
    }

    public bool IsChange => OldPos != NewPos;

    internal static Movement From(TransientUnit unit)
    {
        return new(unit.OldPos, unit.NewPos);
    }
}
