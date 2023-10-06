using NecoBowl.Core.Model;

namespace neco_soft.NecoBowlDefinitions.Card;

public class Chicken : UnitCardModel
{
    public static readonly Chicken Instance = new();

    public override int Cost => 1;
    public override UnitModel Model => Unit.Chicken.Instance;
}
