using neco_soft.NecoBowlCore.Action;
using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlCore.Model;

public abstract class NecoUnitModel
{
    public abstract string InternalName { get; }
    public abstract string Name { get; }
    public abstract int Health { get; }
    public abstract int Power { get; }
    public abstract IEnumerable<NecoUnitAction> Actions { get; }
    public abstract string BehaviorDescription { get; }

    public virtual IEnumerable<NecoUnitTag> Tags { get; }

    public virtual ReactionDict? Reactions { get; } = null;
}

public class ReactionDict : Dictionary<Type, Func<NecoUnitId, IEnumerable<NecoPlayfieldMutation>>>
{ }