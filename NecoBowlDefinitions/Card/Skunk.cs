using neco_soft.NecoBowlCore.Model;

namespace neco_soft.NecoBowlDefinitions.Card;

public class Skunk : NecoUnitCardModel
{
    public static readonly Skunk Instance = new Skunk();
    
    public override int Cost => 4;
    public override NecoUnitModel Model => Unit.Skunk.Instance;
}