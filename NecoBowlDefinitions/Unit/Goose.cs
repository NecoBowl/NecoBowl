using neco_soft.NecoBowlCore;
using neco_soft.NecoBowlCore.Action;
using neco_soft.NecoBowlCore.Model;
using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlDefinitions.Unit;

/// <summary>
///     General-purpose defensive threat.
/// </summary>
public class Goose : NecoUnitModel
{
    public static readonly Goose Instance = new();

    public override string InternalName => "Goose";
    public override string Name => "Goose";
    public override int Health => 10;
    public override int Power => 5;

    public override IEnumerable<NecoUnitTag> Tags => new[] {
        NecoUnitTag.Carrier
    };

    public override IEnumerable<NecoUnitAction> Actions => new[] {
        new NecoUnitAction.ChaseBall(new[] {
            RelativeDirection.Up,
            RelativeDirection.UpLeft,
            RelativeDirection.UpRight
        })
    };

    public override string BehaviorDescription
        => "predator";
}
