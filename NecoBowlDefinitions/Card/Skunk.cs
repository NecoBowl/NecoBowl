using NecoBowl.Core.Model;

namespace neco_soft.NecoBowlDefinitions.Card;

public class Skunk : UnitCardModel
{
    public static readonly Skunk Instance = new();

    public override int Cost => 4;
    public override UnitModel Model => Unit.Skunk.Instance;
}
