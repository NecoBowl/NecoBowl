using NecoBowl.Core.Tags;

namespace NecoBowl.Core.Model;

public abstract class CardModel
{
    public abstract string Name { get; }
    public virtual string InternalName => Name;
    public abstract int Cost { get; }

    public virtual IEnumerable<CardOptionPermission> OptionPermissions { get; }
        = new List<CardOptionPermission>();
}

public abstract class UnitCardModel : CardModel
{
    public abstract UnitModel Model { get; }

    public sealed override string Name => Model.Name;
}

public class CardModelCustom : CardModel
{
    public CardModelCustom(string name, int cost)
    {
        Name = name;
        Cost = cost;
    }

    public override string Name { get; }
    public override int Cost { get; }

    /// <summary>
    /// Create a new anonymous CardModel from a UnitModel. This should only be used for testing purposes. Please refer to the
    /// <c>Instance</c> property of actual CardModel implementations for game purposes.
    /// </summary>
    public static UnitCardModel FromUnitModel(UnitModel model, int cost = 0)
    {
        return new UnitCardModelCustom(model, cost);
    }

    private class UnitCardModelCustom : UnitCardModel
    {
        public UnitCardModelCustom(UnitModel model, int cost)
        {
            Model = model;
            Cost = cost;
        }

        public override UnitModel Model { get; }
        public override int Cost { get; }
    }
}
