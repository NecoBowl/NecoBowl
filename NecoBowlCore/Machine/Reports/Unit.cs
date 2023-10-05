using System.Collections.Immutable;
using System.ComponentModel;
using NecoBowl.Core.Sport.Play;
using NecoBowl.Core.Tags;

namespace NecoBowl.Core.Machine.Reports;

/// <summary>
/// A read-only immutable copy of a unit at a certain point in a machine.
/// </summary>c
public record Unit : BaseReport
{
    public readonly NecoUnitId Id;
    public readonly int DamageTaken;
    public readonly Unit? Carrier;

    internal Unit(Machine.Unit unit)
    {
        Id = unit.Id;
        DamageTaken = unit.DamageTaken;
        Carrier = unit.Carrier is { } ? new(unit.Carrier) : null;
    }
}
