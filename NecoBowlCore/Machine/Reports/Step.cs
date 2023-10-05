using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using NecoBowl.Core.Sport.Play;

namespace NecoBowl.Core.Machine.Reports;

public record Step : BaseReport, IEnumerable<Reports.Substep>
{
    private readonly ReadOnlyCollection<Substep> Substeps;

    internal Step(IEnumerable<SubstepContents> substeps)
    {
        Substeps = substeps.Select(s => new Reports.Substep(s)).ToList().AsReadOnly();
    }

    public IEnumerator<Reports.Substep> GetEnumerator()
    {
        return Substeps.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    internal IEnumerable<Mutation> GetAllMutations()
    {
        return Substeps.SelectMany(s => s.Mutations, (_, mut) => mut);
    }

    internal IEnumerable<TransientUnit> GetAllMovements()
    {
        return Substeps.SelectMany(s => s.Movements, (_, m) => m);
    }
}
