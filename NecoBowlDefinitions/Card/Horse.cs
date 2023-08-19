using neco_soft.NecoBowlCore.Model;

namespace neco_soft.NecoBowlDefinitions.Card;

public class Horse : NecoUnitCardModel
{
    public static readonly Horse Instance = new();

    public override int Cost => 3;
    public override NecoUnitModel Model => Unit.Horse.Instance;
}
