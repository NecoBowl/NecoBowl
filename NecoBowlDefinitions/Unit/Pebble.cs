using NecoBowl.Core;
using NecoBowl.Core.Machine;
using NecoBowl.Core.Model;
using NecoBowl.Core.Sport.Play;
using NecoBowl.Core.Tags;

namespace neco_soft.NecoBowlDefinitions.Unit;

public class Pebble : UnitModel
{
    public static readonly Pebble Instance = new();

    public override string InternalName => "Pebble";
    public override string Name => "Pebble";
    public override int Health => 6;
    public override int Power => 2;

    public override IEnumerable<BaseBehavior> Actions => new[] { new TranslateUnit(RelativeDirection.Up) };

    public override string BehaviorDescription => "Walks forward.";

    public override IEnumerable<NecoUnitTag> Tags => new[] { NecoUnitTag.Carrier };
}
