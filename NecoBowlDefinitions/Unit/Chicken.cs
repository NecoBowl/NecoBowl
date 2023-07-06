using neco_soft.NecoBowlCore;
using neco_soft.NecoBowlCore.Action;
using neco_soft.NecoBowlCore.Model;
using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlDefinitions.Unit;

public class Chicken : NecoUnitModel
{
    public static readonly Chicken Instance = new();
    
    public override string Name => "Chicken";
    public override int Power => 1;

    public override IReadOnlyCollection<NecoUnitTag> Tags
        => new NecoUnitTag[] { };

    protected override IEnumerable<NecoPlanModPermission> ModPermissions
        => new NecoPlanModPermission[] { };
    
    public override IEnumerable<NecoUnitAction> Actions
        => new[] { new NecoUnitAction.TranslateUnit(AbsoluteDirection.North) };
}