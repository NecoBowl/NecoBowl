using neco_soft.NecoBowlCore.Action;
using neco_soft.NecoBowlCore.Model;
using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlDefinitions.Unit;

public class Crab : NecoUnitModel
{
    public static readonly Crab Instance = new();

    public override string InternalName => "Crab";
    public override string Name => "Crab";
    public override int Health => 2;
    public override int Power => 2;

    public override IReadOnlyCollection<NecoUnitTag> Tags
        => new[] { NecoUnitTag.Defender };

    public override IEnumerable<NecoUnitAction> Actions
        => new[] { new NecoUnitAction.TranslateUnitCrabwalk() };

    public override string BehaviorDescription
        => $"Moves {Arrow6} or {Arrow2}, whichever brings it closer to the ball.";
}
