using NecoBowl.Core.Model;
using NecoBowl.Core.Tags;

namespace neco_soft.NecoBowlDefinitions.Card;

public class Horse : NecoUnitCardModel
{
    public static readonly Horse Instance = new();

    public override int Cost => 3;
    public override NecoUnitModel Model => Unit.Horse.Instance;

    public override IEnumerable<NecoCardOptionPermission> OptionPermissions { get; }
        = new[] { new NecoCardOptionPermission.InvertRotations() };
}
