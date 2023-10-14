using System.Collections.Immutable;
using NecoBowl.Core.Machine;
using NecoBowl.Core.Model;
using NecoBowl.Core.Sport.Tactics;
using NecoBowl.Core.Tags;

namespace NecoBowl.Core.Reports;

/// <summary>A read-only immutable copy of a unit at a certain point in a machine.</summary>
public record Unit : BaseReport
{
    public readonly Unit? Carrier;
    public readonly int DamageTaken;
    public readonly int Power;
    public readonly NecoUnitId Id;
    public readonly NecoPlayerId OwnerId;
    public readonly ImmutableList<NecoUnitTag> Tags;
    public readonly UnitModel UnitModel;
    public readonly string FullName;
    public readonly int MaxHealth;

    public int CurrentHealth => MaxHealth - DamageTaken;

    internal Unit(Machine.Unit unit)
    {
        Id = unit.Id;
        OwnerId = unit.OwnerId;
        DamageTaken = unit.DamageTaken;
        Power = unit.Power;
        Carrier = unit.Carrier is { } ? new(unit.Carrier) : null;
        Tags = unit.Tags.ToImmutableList();
        UnitModel = unit.UnitModel;
        FullName = unit.FullName;
        MaxHealth = unit.MaxHealth;
    }
}
