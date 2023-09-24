namespace neco_soft.NecoBowlCore.Action;

public abstract partial class NecoPlayfieldMutation
{
    internal static readonly Action<BaseMutation, NecoSubstepContext, NecoField>[] ExecutionOrder = {
        (m, s, f) => m.Prepare(s, f.AsReadOnly()),
        (m, s, f) => m.Pass1Mutate(f),
        (m, s, f) => m.Pass2Mutate(f),
        (m, s, f) => m.Pass3Mutate(f)
    };

    public abstract string Description { get; }

    internal virtual NecoUnitId[] ExtractedUnits => new NecoUnitId[] { };

    public override string ToString()
    {
        return $"[{Description}]";
    }

    /// <summary>
    ///     Perform any pre-pass checks.
    /// </summary>
    /// <returns>Whether this mutation should be removed from the processing queue.</returns>
    internal virtual bool Prepare(NecoSubstepContext context, ReadOnlyNecoField field)
    {
        return false;
    }

    public sealed class MovementMutation : NecoPlayfieldMutation
    {
        public NecoUnitMovement Movement;

        public MovementMutation(NecoUnitMovement movement)
        {
            Movement = movement;
        }

        public Vector2i OldPos => Movement.OldPos;
        public Vector2i NewPos => Movement.NewPos;
        public NecoUnitId Subject => Movement.UnitId;

        public override string Description
            => $"{Subject} moves from {OldPos} to {NewPos}";
    }

    public abstract class BaseMutation : NecoPlayfieldMutation
    {
        public readonly NecoUnitId Subject;

        protected BaseMutation(NecoUnitId subject)
        {
            Subject = subject;
        }

        internal virtual void Pass1Mutate(NecoField field)
        { }

        internal virtual void Pass2Mutate(NecoField field)
        { }

        internal virtual void Pass3Mutate(NecoField field)
        { }

        internal virtual void EarlyMutate(NecoField field, NecoSubstepContext substepContext)
        { }

        internal virtual IEnumerable<NecoPlayfieldMutation> GetResultantMutations(ReadOnlyNecoField field)
        {
            yield break;
        }
    }
}

internal class NecoSubstepContext
{
    private readonly Dictionary<NecoUnitId, NecoPlayfieldMutation.MovementMutation> Dict;
    private readonly List<NecoPlayfieldMutation.BaseMutation> Mutations;

    public NecoSubstepContext(Dictionary<NecoUnitId, NecoPlayfieldMutation.MovementMutation> dict,
                              List<NecoPlayfieldMutation.BaseMutation> mutations)
    {
        Dict = dict;
        Mutations = mutations;
    }

    public void AddEntry(NecoUnitId unit, NecoUnitMovement movement)
    {
        Dict[unit] = new(movement);
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
    { }

    public NecoPlayfieldMutationException(string message) : base(message)
    { }

    public NecoPlayfieldMutationException(string message, Exception inner) : base(message, inner)
    { }
}
