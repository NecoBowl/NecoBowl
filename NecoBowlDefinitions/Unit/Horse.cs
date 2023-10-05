using NecoBowl.Core;
using NecoBowl.Core.Model;
using NecoBowl.Core.Sport.Play;
using NecoBowl.Core.Tags;

namespace neco_soft.NecoBowlDefinitions.Unit;

public class Horse : NecoUnitModel
{
    public static readonly Horse Instance = new();

    public override string InternalName => "Horse";
    public override string Name => "Horse";
    public override int Health => 7;
    public override int Power => 4;

    public override IEnumerable<Behavior> Actions => new[] {
        new Behavior.TranslateUnit(RelativeDirection.Up),
        new Behavior.TranslateUnit(RelativeDirection.Up)
            .Chain(new ApplyMod(new NecoUnitMod.Rotate(2))),
        new Behavior.TranslateUnit(RelativeDirection.Up)
            .Chain(new ApplyMod(new NecoUnitMod.Rotate(-2)))
    };

    public override string BehaviorDescription
        => $"Moves {Arrow0}. After the second move, rotates {Arrow2}, and after the third move, rotates {Arrow6}.";

    public override IEnumerable<NecoUnitTag> Tags { get; }
        = new[] { NecoUnitTag.Bossy, NecoUnitTag.Carrier };
}
