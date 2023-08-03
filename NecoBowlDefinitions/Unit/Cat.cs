using System.Security.AccessControl;

using neco_soft.NecoBowlCore;
using neco_soft.NecoBowlCore.Action;
using neco_soft.NecoBowlCore.Model;
using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlDefinitions.Unit;

/// <summary>
/// Diagonally-moving quick unit.
/// </summary>
public class Cat : NecoUnitModel
{
    public static readonly Cat Instance = new Cat();

    public override string InternalName => "Cat";
    public override string Name => "Cat";
    public override int Health => 5;
    public override int Power => 3;
    public override IEnumerable<NecoUnitTag> Tags 
        => new NecoUnitTag[] { };
    public override IEnumerable<NecoUnitAction> Actions 
        => new NecoUnitAction[] { new NecoUnitAction.TranslateUnit(RelativeDirection.Up) };
}