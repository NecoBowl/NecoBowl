using neco_soft.NecoBowlCore;

namespace neco_soft.NecoBowlDefinitions;

internal static class Common
{
    public static readonly string Arrow0 = RelativeDirection.Up.ToArrowGlyph();
    public static readonly string Arrow1 = RelativeDirection.UpRight.ToArrowGlyph();
    public static readonly string Arrow2 = RelativeDirection.Right.ToArrowGlyph();
    public static readonly string Arrow3 = RelativeDirection.DownRight.ToArrowGlyph();
    public static readonly string Arrow4 = RelativeDirection.Down.ToArrowGlyph();
    public static readonly string Arrow5 = RelativeDirection.DownLeft.ToArrowGlyph();
    public static readonly string Arrow6 = RelativeDirection.Left.ToArrowGlyph();
    public static readonly string Arrow7 = RelativeDirection.UpLeft.ToArrowGlyph();
}
