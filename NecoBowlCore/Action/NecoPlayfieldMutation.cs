using System;
using System.Reflection.Metadata.Ecma335;

using neco_soft.NecoBowlCore.Tags;

using NLog.Config;

namespace neco_soft.NecoBowlCore.Action;

public abstract partial class NecoPlayfieldMutation
{
    internal virtual void Prepare(NecoSubstepContext context, ReadOnlyNecoField field) { }

    internal static readonly Action<BaseMutation,  NecoSubstepContext, NecoField>[] ExecutionOrder = new Action<BaseMutation, NecoSubstepContext, NecoField>[] {
        (m, s, f) => m.Prepare(s, f.AsReadOnly()),
        (m, s, f) => m.Pass1Mutate(f),
        (m, s, f) => m.Pass2Mutate(f),
        (m, s, f) => m.Pass3Mutate(f),
    };

    public sealed class MovementMutation : NecoPlayfieldMutation
    {
        public readonly NecoUnitId Subject;
        public readonly Vector2i OldPos, NewPos;

        public MovementMutation(NecoUnitId subject, Vector2i oldPos, Vector2i newPos)
        {
            Subject = subject;
            OldPos = oldPos;
            NewPos = newPos;
        }
    }

    public abstract class BaseMutation : NecoPlayfieldMutation
    {
        internal virtual void Pass1Mutate(NecoField field) { }
        internal virtual void Pass2Mutate(NecoField field) { }
        internal virtual void Pass3Mutate(NecoField field) { }

        internal virtual IEnumerable<BaseMutation> AddMutations(ReadOnlyNecoField field)
        {
            yield break;
        }
    }
}

internal class NecoSubstepContext
{
    private readonly List<NecoPlayfieldMutation.BaseMutation> Mutations;
    public readonly IEnumerable<NecoUnitMovement> Movements;
    
    public IReadOnlyList<NecoPlayfieldMutation> GetMutations() => Mutations;
    public IEnumerable<NecoUnitMovement> GetMovements() => Movements;

    public NecoSubstepContext(List<NecoPlayfieldMutation.BaseMutation> mutations, IEnumerable<NecoUnitMovement> movements)
    {
        Mutations = mutations;
        Movements = movements;
    }

    public void AddMutation(NecoPlayfieldMutation.BaseMutation mutation)
    {
        Mutations.Add(mutation);
    }
}

public class NecoPlayfieldMutationException : ApplicationException
{
    public NecoPlayfieldMutationException() { }
    public NecoPlayfieldMutationException(string message) : base(message) { }
    public NecoPlayfieldMutationException(string message, Exception inner) : base(message, inner) { }
}