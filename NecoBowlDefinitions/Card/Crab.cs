using neco_soft.NecoBowlCore.Model;

namespace neco_soft.NecoBowlDefinitions.Card;

public class Crab : NecoUnitCardModel
{
    public static readonly Crab Instance = new Crab();
    
    public override int Cost => 2;
    public override NecoUnitModel Model => Unit.Crab.Instance;
}