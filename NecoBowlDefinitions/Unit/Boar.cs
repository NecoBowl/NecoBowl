using NecoBowl.Core;
using NecoBowl.Core.Model;
using NecoBowl.Core.Sport.Play;
using NecoBowl.Core.Tags;

namespace neco_soft.NecoBowlDefinitions.Unit;

public class Boar : NecoUnitModel
{
    public static readonly Boar Instance = new();

    public override string InternalName => "Boar";
    public override string Name => "Boar";
    public override int Health => 5;
    public override int Power => 2;

    public override IReadOnlyCollection<NecoUnitTag> Tags
        => new[] { NecoUnitTag.Pusher };

    public override IEnumerable<NecoUnitAction> Actions
        => new[] { new NecoUnitAction.TranslateUnit(RelativeDirection.Up) };

    public override string BehaviorDescription
        => $"Moves {Arrow0}.";
}
