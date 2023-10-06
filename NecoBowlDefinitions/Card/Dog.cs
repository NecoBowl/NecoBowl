using NecoBowl.Core.Model;

namespace neco_soft.NecoBowlDefinitions.Card;

public class Dog : UnitCardModel
{
    public static readonly Dog Instance = new();

    public override int Cost => 2;
    public override UnitModel Model => Unit.Dog.Instance;
}
