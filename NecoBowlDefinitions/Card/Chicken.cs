using neco_soft.NecoBowlCore.Model;

namespace neco_soft.NecoBowlDefinitions.Card;

public class Chicken : NecoUnitCardModel
{
    public static readonly Chicken Instance = new Chicken();
    
    public override int Cost => 1;
    public override NecoUnitModel Model => Unit.Chicken.Instance;
}