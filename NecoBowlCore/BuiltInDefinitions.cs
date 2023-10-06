using NecoBowl.Core.Model;
using NecoBowl.Core.Sport.Play;
using NecoBowl.Core.Tags;

namespace NecoBowl.Core;

public class BuiltInDefinitions
{
    public class Ball : UnitModel
    {
        public static readonly Ball Instance = new();

        public override string InternalName => "Ball";
        public override string Name => "The Ball";
        public override int Health => 1;
        public override int Power => 0;

        public override string BehaviorDescription => "Does nothing.";

        public override IReadOnlyCollection<NecoUnitTag> Tags
            => new[] { NecoUnitTag.TheBall, NecoUnitTag.Item };

        public override IEnumerable<Behavior> Actions
            => new[] { new DoNothing() };
    }
}
