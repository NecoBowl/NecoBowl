using neco_soft.NecoBowlCore;
using neco_soft.NecoBowlCore.Action;
using neco_soft.NecoBowlCore.Model;
using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlDefinitions.Unit;

public class Crab : NecoUnitModel
{
    public static readonly Crab Instance = new();

    public override string InternalName => "Crab";
    public override string Name => "Crab";
    public override int Health => 5;
    public override int Power => 2;

    public override IReadOnlyCollection<NecoUnitTag> Tags
        => new NecoUnitTag[] { NecoUnitTag.Defender };

    public override IEnumerable<NecoUnitAction> Actions
        => new[] { new NecoUnitAction.TranslateUnitCrabwalk() };

    public override string BehaviorDescription
        => "Walks horizontally to align with the ball.";
}