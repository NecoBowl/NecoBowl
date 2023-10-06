using System.Collections;
using System.Collections.ObjectModel;
using NecoBowl.Core.Sport.Play;

namespace NecoBowl.Core.Machine.Reports;

public record Step : BaseReport, IEnumerable<Substep>
{
    private readonly ReadOnlyCollection<Substep> Substeps;

    internal Step(IEnumerable<SubstepContents> substeps)
    {
        Substeps = substeps.Select(s => new Substep(s.Mutations, s.Movements)).ToList().AsReadOnly();
    }

    public IEnumerator<Substep> GetEnumerator()
    {
        return Substeps.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IEnumerable<Mutation> GetAllMutations()
    {
        return Substeps.SelectMany(s => s.Mutations, (_, mut) => mut);
    }

    public IEnumerable<Movement> GetAllMovements()
    {
        return Substeps.SelectMany(s => s.Movements, (_, kv) => kv.Value);
    }
}
