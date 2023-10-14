using NecoBowl.Core.Machine;
using NecoBowl.Core.Tags;
using Playfield = NecoBowl.Core.Reports.Playfield;
using Unit = NecoBowl.Core.Reports.Unit;

namespace NecoBowl.Core.Model;

public abstract class UnitModel
{
    public abstract string InternalName { get; }
    public abstract string Name { get; }
    public abstract int Health { get; }
    public abstract int Power { get; }
    public abstract IEnumerable<BaseBehavior> Actions { get; }
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

public delegate IEnumerable<BaseMutation> MutationReaction<in T>(
    Unit unit,
    Playfield field,
    T mutation);
