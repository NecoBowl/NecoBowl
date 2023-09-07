using neco_soft.NecoBowlCore;
using neco_soft.NecoBowlCore.Action;
using neco_soft.NecoBowlCore.Model;
using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlDefinitions.Unit;

public class Rooster : NecoUnitModel
{
    public static readonly Rooster Instance = new();

    public override string Name => "Rooster";
    public override string InternalName => "Rooster";

    public override int Health => 6;
    public override int Power => 2;

    public override IEnumerable<NecoUnitAction> Actions
        => new[] { new NecoUnitAction.TranslateUnit(RelativeDirection.Up) };

    public override string BehaviorDescription => "Moves forward.";

    public override IEnumerable<NecoUnitTag> Tags { get; }
        = new[] { NecoUnitTag.Bossy };
}
