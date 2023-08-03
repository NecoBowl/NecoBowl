using System;
using System.Collections.Generic;
using System.Linq;

namespace neco_soft.NecoBowlCore.Tactics;

public readonly record struct NecoPlayerId()
{
    public readonly Guid Value = Guid.NewGuid();
    public bool IsNeutral => Value == default;
}

public class NecoPlayer
{
    public readonly NecoPlayerId Id = new();
    public bool IsNeutral => Id.IsNeutral;
}

public record class NecoPlayerPair(NecoPlayer Offense, NecoPlayer Defense)
{
    public IEnumerable<NecoPlayer> Enumerate() => new[] { Offense, Defense };

    public NecoPlayer this[NecoPlayerRole role] => FromRole(role);

    public NecoPlayer? PlayerByIdOrNull(NecoPlayerId playerId)
        => Enumerate().SingleOrDefault(p => p.Id == playerId);

    public NecoPlayerRole RoleOf(NecoPlayerId playerId) 
        => Enum.GetValues<NecoPlayerRole>().Single(v 
            => Enumerate().Single(p 
                => p.Id == playerId).Id == FromRole(v).Id);
    
    public NecoPlayer FromRole(NecoPlayerRole role) => role switch {
        NecoPlayerRole.Offense => Offense,
        NecoPlayerRole.Defense => Defense,
        _ => throw new()
    };

    public NecoPlayerPair()
        : this(new(), new())
    { }
}

public enum NecoPlayerRole
{
    Offense,
    Defense
}