using System.Collections.Immutable;
using NecoBowl.Core.Sport.Play;

namespace NecoBowl.Core.Machine.Reports;

public class Substep
{
    public readonly IImmutableList<Mutation> Mutations;
    public readonly IImmutableDictionary<NecoUnitId, Reports.Movement> Movements;

    public Substep(IEnumerable<Mutation> mutations, IDictionary<NecoUnitId, Reports.Movement> movements)
    {
        Mutations = mutations.ToImmutableList();
        Movements = movements.ToImmutableDictionary();
    }

    internal Substep(IEnumerable<Mutation> mutations, IEnumerable<TransientUnit> movements)
        : this(mutations, movements.ToDictionary(m => m.Unit.Id, m => new Movement(m.OldPos, m.NewPos)))
    { }
}
