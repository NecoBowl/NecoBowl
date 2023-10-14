using NecoBowl.Core;
using NecoBowl.Core.Model;
using NecoBowl.Core.Tags;

namespace neco_soft.NecoBowlDefinitions.Card;

public class Pebble : UnitCardModel
{
    public static readonly Pebble Instance = new();

    public override int Cost => 3;
    public override UnitModel Model => Unit.Pebble.Instance;

    public override IEnumerable<CardOptionPermission> OptionPermissions { get; }
        = new[] {
            new CardOptionPermission.Rotate(
                new[] {
                    RelativeDirection.UpLeft,
                    RelativeDirection.UpRight,
                    RelativeDirection.DownRight,
                    RelativeDirection.DownLeft,
                },
                RelativeDirection.UpLeft),
        };
}
