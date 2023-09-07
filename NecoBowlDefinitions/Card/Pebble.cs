using neco_soft.NecoBowlCore;
using neco_soft.NecoBowlCore.Model;
using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlDefinitions.Card;

public class Pebble : NecoUnitCardModel
{
    public static readonly Pebble Instance = new();

    public override int Cost => 3;
    public override NecoUnitModel Model => Unit.Pebble.Instance;

    public override IEnumerable<NecoCardOptionPermission> OptionPermissions { get; }
        = new[] {
            new NecoCardOptionPermission.Rotate(
                new[] {
                    RelativeDirection.UpLeft,
                    RelativeDirection.UpRight,
                    RelativeDirection.DownRight,
                    RelativeDirection.DownLeft
                },
                RelativeDirection.UpLeft)
        };
}
