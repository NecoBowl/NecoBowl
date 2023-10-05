using NecoBowl.Core;
using NecoBowl.Core.Model;
using NecoBowl.Core.Sport.Play;
using NecoBowl.Core.Tags;

namespace neco_soft.NecoBowlDefinitions.Card;

public class Goose : NecoUnitCardModel
{
    public static readonly Goose Instance = new();

    public override int Cost => 4;
    public override NecoUnitModel Model => Unit.Goose.Instance;

    public override IEnumerable<NecoCardOptionPermission> OptionPermissions { get; }
        = new[] {
            new NecoCardOptionPermission.DirectionOptionPermission(
                RelativeDirection.Up,
                ChaseBall.Option_FallbackDirecttion,
                new[] { RelativeDirection.Up, RelativeDirection.UpLeft, RelativeDirection.UpRight })
        };
}
