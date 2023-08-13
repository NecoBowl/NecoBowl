namespace neco_soft.NecoBowlCore.Action;

public abstract partial class NecoPlayfieldMutation
{
    internal static readonly Action<BaseMutation, NecoSubstepContext, NecoField>[] ExecutionOrder = {
        (m, s, f) => m.Prepare(s, f.AsReadOnly()),
        (m, s, f) => m.Pass1Mutate(f),
        (m, s, f) => m.Pass2Mutate(f),
        (m, s, f) => m.Pass3Mutate(f)
    };

    internal virtual void Prepare(NecoSubstepContext context, ReadOnlyNecoField field)
    { }

    public sealed class MovementMutation : NecoPlayfieldMutation
    {
        public readonly Vector2i OldPos, NewPos;
        public readonly NecoUnitId Subject;

        public MovementMutation(NecoUnitId subject, Vector2i oldPos, Vector2i newPos)
        {
            Subject = subject;
            OldPos = oldPos;
            NewPos = newPos;
        }
    }

    public abstract class BaseMutation : NecoPlayfieldMutation
    {
        internal virtual void Pass1Mutate(NecoField field)
        { }

        internal virtual void Pass2Mutate(NecoField field)
        { }

        internal virtual void Pass3Mutate(NecoField field)
        { }

        internal virtual IEnumerable<BaseMutation> AddMutations(ReadOnlyNecoField field)
        {
            yield break;
        }
    }
}

internal class NecoSubstepContext
{
    public readonly IEnumerable<NecoUnitMovement> Movements;
    private readonly List<NecoPlayfieldMutation.BaseMutation> Mutations;

    public NecoSubstepContext(List<NecoPlayfieldMutation.BaseMutation> mutations,
        IEnumerable<NecoUnitMovement> movements)
    {
        Mutations = mutations;
        Movements = movements;
    }

    public IReadOnlyList<NecoPlayfieldMutation> GetMutations()
    {
        return Mutations;
    }

    public IEnumerable<NecoUnitMovement> GetMovements()
    {
        return Movements;
    }

    public void AddMutation(NecoPlayfieldMutation.BaseMutation mutation)
    {
        Mutations.Add(mutation);
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