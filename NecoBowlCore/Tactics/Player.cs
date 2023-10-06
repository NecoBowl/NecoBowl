using System.Diagnostics.CodeAnalysis;

namespace NecoBowl.Core.Sport.Tactics;

[SuppressMessage("ReSharper", "EmptyConstructor")]
public readonly record struct NecoPlayerId()
{
    public readonly Guid Value = Guid.NewGuid();
    public bool IsNeutral => Value == default;
}

public class Player
{
    public static readonly Player NeutralPlayer = new(default);
    public readonly NecoPlayerId Id = new();

    public Player()
    {
    }

    internal Player(NecoPlayerId id)
    {
        Id = id;
    }

    public bool IsNeutral => Id.IsNeutral;
}

public record class NecoPlayerPair(Player Offense, Player Defense)
{
    public NecoPlayerPair()
        : this(new(), new())
    {
    }

    public Player this[NecoPlayerRole role] => FromRole(role);

    public IEnumerable<Player> Enumerate()
    {
        return new[] { Offense, Defense };
    }

    public Player? PlayerByIdOrNull(NecoPlayerId playerId)
    {
        return Enumerate().SingleOrDefault(p => p.Id == playerId);
    }

    public NecoPlayerRole RoleOf(NecoPlayerId playerId)
    {
        return Enum.GetValues<NecoPlayerRole>()
            .Single(
                v
                    => Enumerate()
                        .Single(
                            p
                                => p.Id == playerId)
                        .Id == FromRole(v).Id);
    }

    public Player FromRole(NecoPlayerRole role)
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
