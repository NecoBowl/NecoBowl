using neco_soft.NecoBowlCore;
using neco_soft.NecoBowlCore.Action;
using neco_soft.NecoBowlCore.Model;
using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlDefinitions.Unit;

public class Boar : NecoUnitModel
{
    public static readonly Boar Instance = new();
    
    public override string Name => "Boar";
    public override int Power => 1;

    public override IReadOnlyCollection<NecoUnitTag> Tags
        => new[] { NecoUnitTag.Pusher };
    protected override IEnumerable<NecoCardOptionPermission> ModPermissions
        => new[] { new NecoCardOptionPermission.Rotate(new[] { 0, 2, 4, 6 }) };
    
    public override IEnumerable<NecoUnitAction> Actions
        => new[] { new NecoUnitAction.TranslateUnit(AbsoluteDirection.North) };
}