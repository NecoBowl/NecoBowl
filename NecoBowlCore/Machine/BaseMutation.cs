namespace NecoBowl.Core.Machine;

/// <summary>
/// Represents an event that causes a change in the board state. Mutations can be created from anywhere, but they mainly
/// come from two sources:
/// <list type="number">
/// <item>Instances of <see cref="BaseBehavior" />, which are created and run at the beginning of a step.</item>
/// <item>Other mutations.</item>
/// </list>
/// </summary>
/// <remarks>
/// Mutations are assumed to be immutable and stateless. They should not contain reference types as members; instead, use
/// the associated <c>Id</c> type for that object. Constructors should take
/// <see cref="Core.Reports.Unit">Reports.Unit</see> as parameters so that they can be made public and can be called from
/// within reactions of unit models.
/// </remarks>
public abstract class BaseMutation
{
    internal static readonly Action<BaseMutation, IPlayfieldChangeReceiver, Playfield>[] ExecutionOrder = {
        (m, s, f) => m.Pass1Mutate(f), (m, s, f) => m.Pass2Mutate(f), (m, s, f) => m.Pass3Mutate(f),
    };

    public readonly NecoUnitId Subject;

    protected internal BaseMutation(NecoUnitId subject)
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
    internal virtual bool Prepare(IPlayfieldChangeReceiver context, ReadOnlyPlayfield field)
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

    internal virtual void EarlyMutate(Playfield field, IPlayfieldChangeReceiver substepContext)
    {
    }

    internal virtual IEnumerable<BaseMutation> GetResultantMutations(ReadOnlyPlayfield field)
    {
        yield break;
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
