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
        => new NecoUnitAction[] { new NecoUnitAction.TranslateUnit(RelativeDirection.Up) };

    public override IEnumerable<NecoUnitTag> Tags => new[] { NecoUnitTag.Carrier };

    private IEnumerable<NecoPlayfieldMutation.BaseMutation> OnPickedUpBall(IEnumerable<NecoPlayfieldMutation> source)
    {
        yield break;
        // TODO Set up this reaction
    }
}