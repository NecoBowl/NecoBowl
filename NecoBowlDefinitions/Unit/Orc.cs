using neco_soft.NecoBowlCore;
using neco_soft.NecoBowlCore.Action;
using neco_soft.NecoBowlCore.Model;
using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlDefinitions.Unit;

public class Orc : NecoUnitModel
{
    public static readonly Orc Instance = new();
    
    public override string Name => "Orc Footman";
    public override int Power => 1;

    public override IReadOnlyCollection<NecoUnitTag> Tags
        => new[] { NecoUnitTag.Pusher };

    public override IEnumerable<NecoUnitPlanMod> AllowedMods
        => new[] { NecoUnitPlanMod.Rotate };

    public override IEnumerable<NecoUnitAction> Actions
        => new[] { new NecoUnitAction.TranslateUnit(AbsoluteDirection.North) };
    
    public override void SetupEventHandlers(NecoUnit subject, NecoUnitEventHandler handler)
    { }
}