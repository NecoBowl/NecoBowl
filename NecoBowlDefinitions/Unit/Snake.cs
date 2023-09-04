using neco_soft.NecoBowlCore;
using neco_soft.NecoBowlCore.Action;
using neco_soft.NecoBowlCore.Model;

namespace neco_soft.NecoBowlDefinitions.Unit;

public class Snake : NecoUnitModel
{
    public static readonly Snake Instance = new();

    public override string InternalName => "Snake";
    public override string Name => "Snake";
    public override int Health => 5;
    public override int Power => 2;

    public override IEnumerable<NecoUnitAction> Actions => new[] {
        new NecoUnitAction.TranslateUnit(RelativeDirection.Right),
        new NecoUnitAction.TranslateUnit(RelativeDirection.Up),
        new NecoUnitAction.TranslateUnit(RelativeDirection.Left),
        new NecoUnitAction.TranslateUnit(RelativeDirection.Up)
    };

    public override string BehaviorDescription => "Moves in an \"S\" motion, starting by moving right.";
}
