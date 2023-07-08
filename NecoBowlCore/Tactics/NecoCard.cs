using neco_soft.NecoBowlCore.Model;
using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlCore.Tactics;

public class NecoCard
{
    public readonly NecoCardModel CardModel;
    public int Cost;
    public readonly NecoCardOptionSet Options = new();

    public NecoCard(NecoCardModel cardModel)
    {
        CardModel = cardModel;
        Cost = CardModel.Cost;
    }
}

public class NecoUnitCard : NecoCard
{
    public NecoUnitModel UnitModel
        => ((NecoUnitCardModel)CardModel).Model;
    
    public NecoUnitCard(NecoUnitCardModel cardModel) : base(cardModel)
    { }
}

public class NecoCardOptionSet : HashSet<NecoCardOptionValue>
{
    public NecoCardOptionSet()
        : base(new PlanModPermissionEqualityComparer())
    { }
    
    private class PlanModPermissionEqualityComparer : IEqualityComparer<NecoCardOptionValue>
    {
        public bool Equals(NecoCardOptionValue? x, NecoCardOptionValue? y)
            => x?.GetType() == y?.GetType();

        public int GetHashCode(NecoCardOptionValue obj)
            => obj.GetHashCode();
    }
}