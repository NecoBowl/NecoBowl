using NecoBowl.Core.Machine;
using NecoBowl.Core.Model;
using NecoBowl.Core.Sport.Play;
using NecoBowl.Core.Tags;

namespace neco_soft.NecoBowlDefinitions.Unit;

/// <summary>High-commitment, high-coverage control unit.</summary>
public class Skunk : UnitModel
{
    public static readonly Skunk Instance = new();

    public override string InternalName => "Skunk";
    public override string Name => "Skunk";
    public override int Health => 10;
    public override int Power => 4;

    public override IEnumerable<NecoUnitTag> Tags => new[] { NecoUnitTag.UNIMPL_Smelly };

    public override IEnumerable<BaseBehavior> Actions => new[] { new DoNothing() };

    public override string BehaviorDescription
        => "Smelly with an imposing presence.";
}
