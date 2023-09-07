using neco_soft.NecoBowlCore;
using neco_soft.NecoBowlCore.Action;
using neco_soft.NecoBowlCore.Model;
using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlDefinitions.Card;

public class Goose : NecoUnitCardModel
{
    public static readonly Goose Instance = new();

    public override int Cost => 4;
    public override NecoUnitModel Model => Unit.Goose.Instance;

    public override IEnumerable<NecoCardOptionPermission> OptionPermissions { get; }
        = new[] {
            new NecoCardOptionPermission.DirectionOption(
                RelativeDirection.Up,
                NecoUnitAction.ChaseBall.Option_FallbackDirecttion,
                new[] { RelativeDirection.Up, RelativeDirection.UpLeft, RelativeDirection.UpRight })
        };
}
