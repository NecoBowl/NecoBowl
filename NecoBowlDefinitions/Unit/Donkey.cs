using NecoBowl.Core;
using NecoBowl.Core.Model;
using NecoBowl.Core.Sport.Play;
using NecoBowl.Core.Tags;

namespace neco_soft.NecoBowlDefinitions.Unit;

public class Donkey : NecoUnitModel
{
    public static readonly Donkey Instance = new();

    public override string InternalName => "Donkey";
    public override string Name => "Donkey";
    public override int Health => 4;
    public override int Power => 2;

    public override IEnumerable<NecoUnitAction> Actions => new[] {
        new NecoUnitAction.TranslateUnit(RelativeDirection.Up).Chain(
            new NecoUnitAction.ApplyMod(new NecoUnitMod.Rotate(2))),
        new NecoUnitAction.TranslateUnit(RelativeDirection.Up).Chain(
            new NecoUnitAction.ApplyMod(new NecoUnitMod.Rotate(-2)))
    };

    public override string BehaviorDescription
        => $"Moves forward. Alternates between rotating {Arrow2} and {Arrow6} after each move.";

    public override IEnumerable<NecoUnitTag> Tags { get; }
        = new[] { NecoUnitTag.Pusher };
}
