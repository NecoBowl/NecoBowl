using neco_soft.NecoBowlCore.Model;
using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlDefinitions.Card;

public class Horse : NecoUnitCardModel
{
    public static readonly Horse Instance = new();

    public override int Cost => 3;
    public override NecoUnitModel Model => Unit.Horse.Instance;

    public override IEnumerable<NecoCardOptionPermission> OptionPermissions { get; }
        = new[] { new NecoCardOptionPermission.FlipX() };
}
