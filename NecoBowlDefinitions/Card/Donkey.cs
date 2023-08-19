using neco_soft.NecoBowlCore.Model;

namespace neco_soft.NecoBowlDefinitions.Card;

public class Donkey : NecoUnitCardModel
{
    public static readonly Donkey Instance = new();

    public override int Cost => 2;
    public override NecoUnitModel Model => Unit.Donkey.Instance;
}
