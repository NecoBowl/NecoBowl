using NecoBowl.Core;
using NecoBowl.Core.Machine;
using NecoBowl.Core.Machine.Mutations;
using NecoBowl.Core.Model;
using NecoBowl.Core.Sport.Play;
using NecoBowl.Core.Tags;

namespace neco_soft.NecoBowlDefinitions.Unit;

public class Dog : UnitModel
{
    public static readonly Dog Instance = new();

    public override string InternalName => "Dog";
    public override string Name => "Playful Dog";
    public override int Health => 3;
    public override int Power => 0;

    public override string BehaviorDescription
        => "Walks forward. Turns around upon picking up the ball.";

    public override IEnumerable<BaseBehavior> Actions
        => new BaseBehavior[] { new TranslateUnit(RelativeDirection.Up) };

    public override IEnumerable<NecoUnitTag> Tags => new[] { NecoUnitTag.Carrier };

    public override ReactionDict Reactions { get; } = new() {
        new(typeof(UnitPicksUpItem), (unit, _, mut) => OnPicksUp(unit, mut)),
    };

    private static IEnumerable<BaseMutation> OnPicksUp(
        NecoBowl.Core.Reports.Unit unit,
        BaseMutation mutation)
    {
        if (unit.Id == mutation.Subject) {
            yield return new UnitGetsMod(unit, new UnitMod.Rotate(4));
        }
    }
}
