using NecoBowl.Core;
using NecoBowl.Core.Model;
using NecoBowl.Core.Tags;

namespace neco_soft.NecoBowlDefinitions.Card;

public class Pebble : UnitCardModel
{
    public static readonly Pebble Instance = new();

    public override int Cost => 3;
    public override UnitModel Model => Unit.Pebble.Instance;

    public override IEnumerable<NecoCardOptionPermission> OptionPermissions { get; }
        = new[] {
            new NecoCardOptionPermission.Rotate(
                new[] {
                    RelativeDirection.UpLeft,
                    RelativeDirection.UpRight,
                    RelativeDirection.DownRight,
                    RelativeDirection.DownLeft,
                },
                RelativeDirection.UpLeft),
        };
}
