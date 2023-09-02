using neco_soft.NecoBowlCore.Model;
using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlDefinitions.Card;

public class Pebble : NecoUnitCardModel
{
    public static readonly Pebble Instance = new();

    public override int Cost => 4;
    public override NecoUnitModel Model => Unit.Pebble.Instance;

    public override IEnumerable<NecoCardOptionPermission> OptionPermissions { get; }
        = new[] {
            new NecoCardOptionPermission.Rotate(
                new[] {
                    1,
                    3,
                    5,
                    7
                },
                1)
        };
}
