using neco_soft.NecoBowlCore;
using neco_soft.NecoBowlCore.Action;
using neco_soft.NecoBowlCore.Model;
using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlDefinitions.Unit;

public class Boar : NecoUnitModel
{
    public static readonly Boar Instance = new();

    public override string InternalName => "Boar";
    public override string Name => "Boar";
    public override int Health => 5;
    public override int Power => 2;

    public override IReadOnlyCollection<NecoUnitTag> Tags
        => new[] {
            NecoUnitTag.Pusher
        };

    public override IEnumerable<NecoUnitAction> Actions
        => new[] {
            new NecoUnitAction.TranslateUnit(RelativeDirection.Up)
        };

    public override string BehaviorDescription
        => "Walks forward.";
}
