using neco_soft.NecoBowlCore.Action;
using neco_soft.NecoBowlCore.Model;
using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlDefinitions.Unit;

/// <summary>
///     High-commitment, high-coverage control unit.
/// </summary>
public class Skunk : NecoUnitModel
{
    public static readonly Skunk Instance = new();

    public override string InternalName => "Skunk";
    public override string Name => "Skunk";
    public override int Health => 10;
    public override int Power => 4;

    public override IEnumerable<NecoUnitTag> Tags => new[] { NecoUnitTag.UNIMPL_Smelly };

    public override IEnumerable<NecoUnitAction> Actions => new[] { new NecoUnitAction.DoNothing() };

    public override string BehaviorDescription
        => "Smelly with an imposing presence.";
}
