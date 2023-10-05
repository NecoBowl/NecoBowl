using NecoBowl.Core;
using NecoBowl.Core.Model;
using NecoBowl.Core.Sport.Play;
using NecoBowl.Core.Tags;

namespace neco_soft.NecoBowlDefinitions.Unit;

public class Rooster : NecoUnitModel
{
    public static readonly Rooster Instance = new();

    public override string Name => "Rooster";
    public override string InternalName => "Rooster";

    public override int Health => 6;
    public override int Power => 2;

    public override IEnumerable<Behavior> Actions
        => new[] { new Behavior.TranslateUnit(RelativeDirection.Up) };

    public override string BehaviorDescription => "Moves forward.";

    public override IEnumerable<NecoUnitTag> Tags { get; }
        = new[] { NecoUnitTag.Bossy };
}
