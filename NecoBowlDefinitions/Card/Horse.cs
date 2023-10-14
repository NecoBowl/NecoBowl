using NecoBowl.Core.Model;
using NecoBowl.Core.Tags;

namespace neco_soft.NecoBowlDefinitions.Card;

public class Horse : UnitCardModel
{
    public static readonly Horse Instance = new();

    public override int Cost => 3;
    public override UnitModel Model => Unit.Horse.Instance;

    public override IEnumerable<CardOptionPermission> OptionPermissions { get; }
        = new[] { new CardOptionPermission.InvertRotations() };
}
