using NecoBowl.Core;
using NecoBowl.Core.Machine.Behaviors;
using NecoBowl.Core.Model;
using NecoBowl.Core.Sport.Play;
using NecoBowl.Core.Tags;

namespace neco_soft.NecoBowlDefinitions.Unit;

public class Donkey : UnitModel
{
    public static readonly Donkey Instance = new();

    public override string InternalName => "Donkey";
    public override string Name => "Donkey";
    public override int Health => 4;
    public override int Power => 2;

    public override IEnumerable<Behavior> Actions => new[] {
        new TranslateUnit(RelativeDirection.Up).Chain(new ApplyMod(new UnitMod.Rotate(2))),
        new TranslateUnit(RelativeDirection.Up).Chain(new ApplyMod(new UnitMod.Rotate(-2))),
    };

    public override string BehaviorDescription
        => $"Moves forward. Alternates between rotating {Arrow2} and {Arrow6} after each move.";

    public override IEnumerable<NecoUnitTag> Tags { get; }
        = new[] { NecoUnitTag.Pusher };
}
