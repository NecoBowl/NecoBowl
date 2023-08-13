using neco_soft.NecoBowlCore.Model;
using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlDefinitions.Card;

public class Boar : NecoUnitCardModel
{
    public static readonly Boar Instance = new();

    public override int Cost => 2;
    public override NecoUnitModel Model => Unit.Boar.Instance;

    public override IEnumerable<NecoCardOptionPermission> OptionPermissions { get; }
        = new[] { new NecoCardOptionPermission.Rotate(new[] { 0, 2, 4, 6 }, 0) };
}