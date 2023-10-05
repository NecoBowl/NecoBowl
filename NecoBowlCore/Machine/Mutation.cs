using NecoBowl.Core.Machine;

namespace NecoBowl.Core.Sport.Play;

/// <summary>
/// Represents an event that causes a change in the board state. Mutations can be created from anywhere, but they mainly
/// come from two sources:
/// <list type="number">
/// <item>Instances of <see cref="Behavior" />, which are created and run at the beginning of a step.</item>
/// <item>Other mutations.</item>
/// </list>
/// </summary>
public abstract class Mutation
{
    internal static readonly Action<Mutation, NecoSubstepContext, Playfield>[] ExecutionOrder = {
        (m, s, f) => m.Prepare(s, f.AsReadOnly()),
        (m, s, f) => m.Pass1Mutate(f),
        (m, s, f) => m.Pass2Mutate(f),
        (m, s, f) => m.Pass3Mutate(f)
    };

    public readonly NecoUnitId Subject;

    protected Mutation(NecoUnitId subject)
    {
        Subject = subject;
    }

    public abstract string Description { get; }

    internal virtual NecoUnitId[] ExtractedUnits => new NecoUnitId[] { };

    public override string ToString()
    {
        return $"[{Description}]";
    }

    /// <summary>Perform any pre-pass checks.</summary>
    /// <returns><c>true</c> if this unit should be removed from the processing queue, otherwise <c>false</c>.</returns>
    internal virtual bool Prepare(NecoSubstepContext context, ReadOnlyPlayfield field)
    {
        return false;
    }

    internal virtual void Pass1Mutate(Playfield field)
    {
    }

    internal virtual void Pass2Mutate(Playfield field)
    {
    }

    internal virtual void Pass3Mutate(Playfield field)
    {
    }

    internal virtual void EarlyMutate(Playfield field, NecoSubstepContext substepContext)
    {
    }

    internal virtual IEnumerable<Mutation> GetResultantMutations(ReadOnlyPlayfield field)
    {
        yield break;
    }
}

internal class NecoSubstepContext
{
    private readonly Dictionary<NecoUnitId, TransientUnit> Dict;
    private readonly List<Mutation> Mutations;

    public NecoSubstepContext(
        Dictionary<NecoUnitId, TransientUnit> dict,
        List<Mutation> mutations)
    {
        Dict = dict;
        Mutations = mutations;
    }

    public void AddEntry(NecoUnitId unit, TransientUnit movement)
    {
        Dict[unit] = movement;
    }

    public bool HasEntryOfType(NecoUnitId uid, Type type, object? exclusion = null)
    {
        var mut = Mutations.SingleOrDefault(m => m.Subject == uid && m.GetType() == type && m != exclusion);
        return mut is not null;
    }
}

public class NecoPlayfieldMutationException : ApplicationException
{
    public NecoPlayfieldMutationException()
    {
    }

    public NecoPlayfieldMutationException(string message) : base(message)
    {
    }

    public NecoPlayfieldMutationException(string message, Exception inner) : base(message, inner)
    {
    }
}
