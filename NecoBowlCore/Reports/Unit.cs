using System.Collections.Immutable;
using NecoBowl.Core.Machine;
using NecoBowl.Core.Machine.Reports;
using NecoBowl.Core.Tags;

namespace NecoBowl.Core.Reports;

/// <summary>A read-only immutable copy of a unit at a certain point in a machine.</summary>
public record Unit : BaseReport
{
    public readonly Unit? Carrier;
    public readonly int DamageTaken;
    public readonly NecoUnitId Id;
    public readonly ImmutableList<NecoUnitTag> Tags;

    internal Unit(Machine.Unit unit)
    {
        Id = unit.Id;
        DamageTaken = unit.DamageTaken;
        Carrier = unit.Carrier is { } ? new(unit.Carrier) : null;
        Tags = unit.Tags.ToImmutableList();
    }
}
