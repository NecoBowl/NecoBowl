using System.Collections.ObjectModel;

using neco_soft.NecoBowlCore.Model;
using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlDefinitions.Card;

public class Cat : NecoUnitCardModel
{
    public static readonly Cat Instance = new Cat();
    
    public override int Cost => 2;
    public override NecoUnitModel Model => Unit.Cat.Instance;
    
    public override IReadOnlyCollection<NecoCardOptionPermission> OptionPermissions 
        => new NecoCardOptionPermission[] { new NecoCardOptionPermission.Rotate(new int[]{ 1 }, 1) };
}