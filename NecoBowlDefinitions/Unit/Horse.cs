using neco_soft.NecoBowlCore;
using neco_soft.NecoBowlCore.Action;
using neco_soft.NecoBowlCore.Model;
using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlDefinitions.Unit;

public class Horse : NecoUnitModel
{
    public static readonly Horse Instance = new();

    public override string InternalName => "Horse";
    public override string Name => "Horse";
    public override int Health => 7;
    public override int Power => 4;

    public override IEnumerable<NecoUnitAction> Actions => new[] {
        new NecoUnitAction.TranslateUnit(RelativeDirection.Up),
        new NecoUnitAction.TranslateUnit(RelativeDirection.Up)
            .Chain(new NecoUnitAction.ApplyMod(new NecoUnitMod.Rotate(2))),
        new NecoUnitAction.TranslateUnit(RelativeDirection.Up)
            .Chain(new NecoUnitAction.ApplyMod(new NecoUnitMod.Rotate(-2)))
    };

    public override string BehaviorDescription
        => $"Moves {Arrow0}. After the second move, rotates {Arrow2}, and after the third move, rotates {Arrow6}.";

    public override IEnumerable<NecoUnitTag> Tags { get; }
        = new[] { NecoUnitTag.Bossy, NecoUnitTag.Carrier };
}
