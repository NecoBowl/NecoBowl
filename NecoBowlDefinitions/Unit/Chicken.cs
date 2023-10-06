using NecoBowl.Core;
using NecoBowl.Core.Model;
using NecoBowl.Core.Sport.Play;
using NecoBowl.Core.Tags;

namespace neco_soft.NecoBowlDefinitions.Unit;

public class Chicken : UnitModel
{
    public static readonly Chicken Instance = new();

    public override string InternalName => "Chicken";
    public override string Name => "Chicken";
    public override int Health => 3;
    public override int Power => 1;

    public override IReadOnlyCollection<NecoUnitTag> Tags
        => new NecoUnitTag[] { };

    public override IEnumerable<Behavior> Actions
        => new[] { new TranslateUnit(RelativeDirection.Up) };

    public override string BehaviorDescription
        => "Walks forward.";
}
