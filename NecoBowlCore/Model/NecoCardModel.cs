using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlCore.Model;

public readonly record struct NecoCardModelId(Guid Value);

public abstract class NecoCardModel
{
    public abstract string Name { get; }
    public abstract int Cost { get; }
}

public abstract class NecoUnitCardModel : NecoCardModel
{
    public abstract NecoUnitModel Model { get; }

    public override sealed string Name => Model.Name;
}

public class NecoCardModelCustom : NecoCardModel
{
    public override string Name { get; }
    public override int Cost { get; }

    public NecoCardModelCustom(string name, int cost)
    {
        Name = name;
        Cost = cost;
    }
    
    private class NecoUnitCardModelCustom : NecoUnitCardModel
    {
        public override NecoUnitModel Model { get; }
        public override int Cost { get; }

        public NecoUnitCardModelCustom(NecoUnitModel model, int cost)
            : base()
        {
            Model = model;
            Cost = cost;
        }
    }

    /// <summary>
    /// Create a new anonymous CardModel from a UnitModel.
    ///
    /// This should only be used for testing purposes. Please refer to the <c>Instance</c> property of
    /// actual CardModel implementations for game purposes.
    /// </summary>
    public static NecoUnitCardModel FromUnitModel(NecoUnitModel model, int cost = 0)
        => new NecoUnitCardModelCustom(model, cost);
}