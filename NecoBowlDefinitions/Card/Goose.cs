using neco_soft.NecoBowlCore.Model;

namespace neco_soft.NecoBowlDefinitions.Card;

public class Goose : NecoUnitCardModel
{
    public static readonly Goose Instance = new();

    public override int Cost => 4;
    public override NecoUnitModel Model => Unit.Goose.Instance;
}