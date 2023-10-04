using System.Collections;
using System.Collections.ObjectModel;
using NecoBowl.Core.Sport.Play;

namespace NecoBowl.Core.Machine.Reports;

public class StepReport : IEnumerable<SubstepContents>
{
    private readonly ReadOnlyCollection<SubstepContents> Substeps;

    internal StepReport(IEnumerable<SubstepContents> substeps)
    {
        Substeps = substeps.ToList().AsReadOnly();
    }

    public IEnumerator<SubstepContents> GetEnumerator()
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

    internal IEnumerable<NecoUnitMovement> GetAllMovements()
    {
        return Substeps.SelectMany(s => s.Movements, (_, m) => m);
    }
}
