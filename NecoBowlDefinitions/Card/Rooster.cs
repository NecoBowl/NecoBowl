using neco_soft.NecoBowlCore.Model;

namespace neco_soft.NecoBowlDefinitions.Card;

public class Rooster : NecoUnitCardModel
{
    public static readonly Rooster Instance = new();

    public override int Cost => 3;
    public override NecoUnitModel Model => Unit.Rooster.Instance;
}
