using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace NecoBowl.Core.Sport.Tactics;

[SuppressMessage("ReSharper", "EmptyConstructor")]
public record NecoPlayerId
{
    public Guid Id { get; set; }
    
    public override string ToString()
    {
        return $"@P:{Id.ToString().Substring(0, 6)}";
    }
    
    [JsonConstructor]
    public NecoPlayerId(Guid id)
    {
        Id = id;
    }

    public static NecoPlayerId Random() => new(Guid.NewGuid());
}

public class Player
{
    public static readonly Player NeutralPlayer = new(new(Guid.Empty));

    public readonly NecoPlayerId Id;

    public Player()
    {
        Id = NecoPlayerId.Random();
    }

    [JsonConstructor]
    public Player(NecoPlayerId id)
    {
        Id = id;
    }

    public override string ToString()
    {
        return Id.ToString();
    }
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
