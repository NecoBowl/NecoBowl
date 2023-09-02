using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlCore.Model;

public abstract class NecoCardModel
{
    public abstract string Name { get; }
    public abstract int Cost { get; }

    public virtual IEnumerable<NecoCardOptionPermission> OptionPermissions { get; }
        = new List<NecoCardOptionPermission>();
}

public abstract class NecoUnitCardModel : NecoCardModel
{
    public abstract NecoUnitModel Model { get; }

    public sealed override string Name => Model.Name;
}

public class NecoCardModelCustom : NecoCardModel
{
    public NecoCardModelCustom(string name, int cost)
    {
        Name = name;
        Cost = cost;
    }

    public override string Name { get; }
    public override int Cost { get; }

    /// <summary>
    ///     Create a new anonymous CardModel from a UnitModel.
    ///     This should only be used for testing purposes. Please refer to the <c>Instance</c> property of
    ///     actual CardModel implementations for game purposes.
    /// </summary>
    public static NecoUnitCardModel FromUnitModel(NecoUnitModel model, int cost = 0)
    {
        return new NecoUnitCardModelCustom(model, cost);
    }

    private class NecoUnitCardModelCustom : NecoUnitCardModel
    {
        public NecoUnitCardModelCustom(NecoUnitModel model, int cost)
        {
            Model = model;
            Cost = cost;
        }

        public override NecoUnitModel Model { get; }
        public override int Cost { get; }
    }
}
