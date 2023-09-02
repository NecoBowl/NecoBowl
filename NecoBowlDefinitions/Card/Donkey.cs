using neco_soft.NecoBowlCore.Model;
using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlDefinitions.Card;

public class Donkey : NecoUnitCardModel
{
    public static readonly Donkey Instance = new();

    public override int Cost => 2;
    public override NecoUnitModel Model => Unit.Donkey.Instance;

    public override IEnumerable<NecoCardOptionPermission> OptionPermissions
        => new[] { new NecoCardOptionPermission.FlipX() };
}
