using NecoBowl.Core.Model;

namespace neco_soft.NecoBowlDefinitions.Card;

public class Crab : UnitCardModel
{
    public static readonly Crab Instance = new();

    public override int Cost => 2;
    public override UnitModel Model => Unit.Crab.Instance;
}
