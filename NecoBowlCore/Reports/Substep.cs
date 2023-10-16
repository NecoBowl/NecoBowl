using System.Collections.Immutable;
using NecoBowl.Core.Machine;

namespace NecoBowl.Core.Reports;

public class Substep
{
    public readonly IImmutableDictionary<NecoUnitId, Movement> Movements;
    public readonly IImmutableList<BaseMutation> Mutations;

    public Substep(IEnumerable<BaseMutation> mutations, IDictionary<NecoUnitId, Movement> movements)
    {
        Mutations = mutations.ToImmutableList();
        Movements = movements.ToImmutableDictionary();
    }

    internal Substep(IEnumerable<BaseMutation> mutations, IEnumerable<TransientUnit> movements)
        : this(mutations, movements.ToDictionary(m => m.Unit.Id, m => new Movement(m.Unit, m.OldPos, m.NewPos)))
    {
    }
}
