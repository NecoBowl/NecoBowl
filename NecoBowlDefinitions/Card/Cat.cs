using neco_soft.NecoBowlCore.Model;
using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlDefinitions.Card;

public class Cat : NecoUnitCardModel
{
    public static readonly Cat Instance = new();

    public override int Cost => 2;
    public override NecoUnitModel Model => Unit.Cat.Instance;

    public override IEnumerable<NecoCardOptionPermission> OptionPermissions { get; }
        = new[] {
            new NecoCardOptionPermission.Rotate(new[] { 1, 3, 5, 7 }, 1)
        };
}
