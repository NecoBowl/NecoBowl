using neco_soft.NecoBowlCore.Model;
using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlDefinitions.Card;

public class Snake : NecoUnitCardModel
{
    public static readonly Snake Instance = new();

    public override int Cost => 2;
    public override NecoUnitModel Model => Unit.Snake.Instance;

    public override IEnumerable<NecoCardOptionPermission> OptionPermissions { get; }
        = new[] { new NecoCardOptionPermission.FlipX() };
}
