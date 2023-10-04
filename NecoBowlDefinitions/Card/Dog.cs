using NecoBowl.Core.Model;

namespace neco_soft.NecoBowlDefinitions.Card;

public class Dog : NecoUnitCardModel
{
    public static readonly Dog Instance = new();

    public override int Cost => 2;
    public override NecoUnitModel Model => Unit.Dog.Instance;
}
