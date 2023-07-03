namespace neco_soft.NecoBowlCore.Tactics;

public readonly record struct NecoPlayerId()
{
    public readonly Guid Value = Guid.NewGuid();

    public bool IsNeutral => Value == default;
}

public class NecoPlayer
{
    public readonly NecoPlayerId Id = new();
}