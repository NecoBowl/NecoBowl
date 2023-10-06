using NecoBowl.Core;
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

    public override IEnumerable<Behavior> Actions
        => new Behavior[] { new TranslateUnit(RelativeDirection.Up) };

    public override IEnumerable<NecoUnitTag> Tags => new[] { NecoUnitTag.Carrier };

    public override ReactionDict Reactions { get; } = new() {
        new(typeof(UnitPicksUpItem), (unit, _, mut) => OnPicksUp(unit, mut)),
    };

    private static IEnumerable<Mutation> OnPicksUp(
        NecoBowl.Core.Machine.Reports.Unit unit,
        Mutation mutation)
    {
        if (unit.Id == mutation.Subject) {
            yield return new UnitGetsMod(unit.Id, new UnitMod.Rotate(4));
        }
    }
}
