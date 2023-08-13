namespace neco_soft.NecoBowlCore.Tactics;

public readonly record struct NecoPlayerId()
{
    public readonly Guid Value = Guid.NewGuid();
public bool IsNeutral => Value == default;
}

public class NecoPlayer
{
    public static readonly NecoPlayer NeutralPlayer = new(default);
    public readonly NecoPlayerId Id = new();

    public NecoPlayer()
    { }

    internal NecoPlayer(NecoPlayerId id)
    {
        Id = id;
    }

    public bool IsNeutral => Id.IsNeutral;
}

public record class NecoPlayerPair(NecoPlayer Offense, NecoPlayer Defense)
{
    public NecoPlayerPair()
        : this(new(), new())
    { }

    public NecoPlayer this[NecoPlayerRole role] => FromRole(role);

public IEnumerable<NecoPlayer> Enumerate()
{
    return new[] { Offense, Defense };
}

public NecoPlayer? PlayerByIdOrNull(NecoPlayerId playerId)
{
    return Enumerate().SingleOrDefault(p => p.Id == playerId);
}

public NecoPlayerRole RoleOf(NecoPlayerId playerId)
{
    return Enum.GetValues<NecoPlayerRole>()
        .Single(v
            => Enumerate()
                .Single(p
                    => p.Id == playerId)
                .Id == FromRole(v).Id);
}

public NecoPlayer FromRole(NecoPlayerRole role)
{
    return role switch {
        NecoPlayerRole.Offense => Offense,
        NecoPlayerRole.Defense => Defense,
        _ => throw new()
    };
}
}

public enum NecoPlayerRole
{
    Offense,
    Defense
}
