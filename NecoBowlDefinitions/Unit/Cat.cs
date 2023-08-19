using neco_soft.NecoBowlCore;
using neco_soft.NecoBowlCore.Action;
using neco_soft.NecoBowlCore.Model;
using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlDefinitions.Unit;

public class Cat : NecoUnitModel
{
    public static readonly Cat Instance = new();

    public override string InternalName => "Cat";
    public override string Name => "Cat";
    public override int Health => 5;
    public override int Power => 3;

    public override IEnumerable<NecoUnitTag> Tags
        => new[] {
            NecoUnitTag.Carrier
        };

    public override IEnumerable<NecoUnitAction> Actions
        => new NecoUnitAction[] {
            new NecoUnitAction.TranslateUnit(RelativeDirection.Up)
        };

    public override string BehaviorDescription
        => "Walks forward.";
}
