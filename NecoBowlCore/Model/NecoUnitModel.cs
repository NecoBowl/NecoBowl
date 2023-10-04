using NecoBowl.Core.Sport.Play;
using NecoBowl.Core.Tags;

namespace NecoBowl.Core.Model;

public abstract class NecoUnitModel
{
    public abstract string InternalName { get; }
    public abstract string Name { get; }
    public abstract int Health { get; }
    public abstract int Power { get; }
    public abstract IEnumerable<NecoUnitAction> Actions { get; }
    public abstract string BehaviorDescription { get; }

    public virtual IEnumerable<NecoUnitTag> Tags { get; } = Array.Empty<NecoUnitTag>();

    public virtual ReactionDict Reactions { get; } = new();
}

public class ReactionDict : List<ReactionDict.Entry>
{
    public class Entry
    {
        public readonly Type MutationType;
        public readonly MutationReaction<dynamic> Reaction;

        public Entry(Type mutationType, MutationReaction<dynamic> reaction)
        {
            MutationType = mutationType;
            Reaction = reaction;
        }
    }
}

public delegate IEnumerable<Mutation.BaseMutation> MutationReaction<in T>(
    Unit unit,
    ReadOnlyNecoField field,
    T mutation);

public class UnitReactionContext
{
}
