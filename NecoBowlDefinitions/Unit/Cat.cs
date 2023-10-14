using NecoBowl.Core;
using NecoBowl.Core.Machine;
using NecoBowl.Core.Model;
using NecoBowl.Core.Sport.Play;

namespace neco_soft.NecoBowlDefinitions.Unit;

public class Cat : UnitModel
{
    public static readonly Cat Instance = new();

    public override string InternalName => "Cat";
    public override string Name => "Cat";
    public override int Health => 4;
    public override int Power => 3;

    public override IEnumerable<BaseBehavior> Actions
        => new BaseBehavior[] { new TranslateUnit(RelativeDirection.Up) };

    public override string BehaviorDescription
        => $"Moves {Arrow0}.";
}
