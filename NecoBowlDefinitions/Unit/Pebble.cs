using neco_soft.NecoBowlCore;
using neco_soft.NecoBowlCore.Action;
using neco_soft.NecoBowlCore.Model;
using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlDefinitions.Unit;

public class Pebble : NecoUnitModel
{
    public static readonly Pebble Instance = new();

    public override string InternalName => "Pebble";
    public override string Name => "Pebble";
    public override int Health => 6;
    public override int Power => 2;

    public override IEnumerable<NecoUnitAction> Actions => new[] {
        new NecoUnitAction.TranslateUnit(RelativeDirection.Up)
    };

    public override string BehaviorDescription => "Walks forward.";

    public override IEnumerable<NecoUnitTag> Tags => new[] { NecoUnitTag.Carrier };
}
