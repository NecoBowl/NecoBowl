using NecoBowl.Core;
using NecoBowl.Core.Model;
using NecoBowl.Core.Tags;

namespace neco_soft.NecoBowlDefinitions.Card;

public class Boar : UnitCardModel
{
    public static readonly Boar Instance = new();

    public override int Cost => 2;
    public override UnitModel Model => Unit.Boar.Instance;

    public override IEnumerable<NecoCardOptionPermission> OptionPermissions { get; }
        = new[] {
            new NecoCardOptionPermission.Rotate(
                new[] {
                    RelativeDirection.Up, RelativeDirection.Right, RelativeDirection.Down, RelativeDirection.Left,
                }),
        };
}
