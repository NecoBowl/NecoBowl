using NecoBowl.Core;
using NecoBowl.Core.Model;
using NecoBowl.Core.Tags;

namespace neco_soft.NecoBowlDefinitions.Card;

public class Cat : UnitCardModel
{
    public static readonly Cat Instance = new();

    public override int Cost => 1;
    public override UnitModel Model => Unit.Cat.Instance;

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
