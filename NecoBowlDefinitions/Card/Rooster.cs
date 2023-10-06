using NecoBowl.Core.Model;

namespace neco_soft.NecoBowlDefinitions.Card;

public class Rooster : UnitCardModel
{
    public static readonly Rooster Instance = new();

    public override int Cost => 3;
    public override UnitModel Model => Unit.Rooster.Instance;
}
