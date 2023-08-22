using neco_soft.NecoBowlCore;
using neco_soft.NecoBowlCore.Action;
using neco_soft.NecoBowlCore.Model;
using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlDefinitions.Unit;

public class Dog : NecoUnitModel
{
    public static readonly Dog Instance = new();

    public override string InternalName => "Dog";
    public override string Name => "Dog";
    public override int Health => 2;
    public override int Power => 1;

    public override string BehaviorDescription
        => "Walks forward. Turns around upon picking up the ball.";

    public override IEnumerable<NecoUnitAction> Actions
        => new NecoUnitAction[] {
            new NecoUnitAction.TranslateUnit(RelativeDirection.Up)
        };

    public override IEnumerable<NecoUnitTag> Tags => new[] {
        NecoUnitTag.Carrier
    };

    public override ReactionDict Reactions { get; } = new() {
        new(typeof(NecoPlayfieldMutation.UnitPicksUpItem), (unit, field, mut) => OnPicksUp(unit, mut))
    };

    private static IEnumerable<NecoPlayfieldMutation.BaseMutation> OnPicksUp(NecoUnit unit,
                                                                             NecoPlayfieldMutation.BaseMutation
                                                                                 mutation)
    {
        if (unit.Id == mutation.Subject) {
            yield return new NecoPlayfieldMutation.UnitGetsMod(unit.Id, new NecoUnitMod.Rotate(4));
        }
    }
}
