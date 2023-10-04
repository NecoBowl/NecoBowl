using NecoBowl.Core;
using NecoBowl.Core.Model;
using NecoBowl.Core.Sport.Play;
using NecoBowl.Core.Tags;

namespace neco_soft.NecoBowlDefinitions.Unit;

public class Dog : NecoUnitModel
{
    public static readonly Dog Instance = new();

    public override string InternalName => "Dog";
    public override string Name => "Playful Dog";
    public override int Health => 3;
    public override int Power => 0;

    public override string BehaviorDescription
        => "Walks forward. Turns around upon picking up the ball.";

    public override IEnumerable<NecoUnitAction> Actions
        => new NecoUnitAction[] { new NecoUnitAction.TranslateUnit(RelativeDirection.Up) };

    public override IEnumerable<NecoUnitTag> Tags => new[] { NecoUnitTag.Carrier };

    public override ReactionDict Reactions { get; } = new() {
        new(typeof(Mutation.UnitPicksUpItem), (unit, field, mut) => OnPicksUp(unit, mut))
    };

    private static IEnumerable<Mutation.BaseMutation> OnPicksUp(
        NecoBowl.Core.Sport.Play.Unit unit,
        Mutation.BaseMutation
            mutation)
    {
        if (unit.Id == mutation.Subject) {
            yield return new Mutation.UnitGetsMod(unit.Id, new NecoUnitMod.Rotate(4));
        }
    }
}
