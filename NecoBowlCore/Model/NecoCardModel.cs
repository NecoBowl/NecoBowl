namespace neco_soft.NecoBowlCore.Model;

public readonly record struct NecoCardModelId(Guid Value);

public abstract class NecoCardModel
{
    public abstract int Cost { get; }
}

public abstract class NecoUnitCardModel : NecoCardModel
{
    public abstract NecoUnitModel Model { get; }
}