using neco_soft.NecoBowlCore.Action;
using neco_soft.NecoBowlCore.Model;
using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlCore;

public class BuiltInDefinitions
{
    public class Ball : NecoUnitModel
    {
        public static readonly Ball Instance = new();

        public override string InternalName => "Ball";
        public override string Name => "The Ball";
        public override int Health => 1;
        public override int Power => 0;

        public override string BehaviorDescription => "Does nothing.";

        public override IReadOnlyCollection<NecoUnitTag> Tags
            => new[] { NecoUnitTag.TheBall, NecoUnitTag.Item };

        public override IEnumerable<NecoUnitAction> Actions
            => new[] { new NecoUnitAction.DoNothing() };
    }
}
