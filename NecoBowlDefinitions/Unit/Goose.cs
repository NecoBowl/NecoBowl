using NecoBowl.Core;
using NecoBowl.Core.Machine;
using NecoBowl.Core.Model;
using NecoBowl.Core.Sport.Play;
using NecoBowl.Core.Tags;

namespace neco_soft.NecoBowlDefinitions.Unit;

/// <summary>General-purpose defensive threat.</summary>
public class Goose : UnitModel
{
    public static readonly Goose Instance = new();

    public override string InternalName => "Goose";
    public override string Name => "Goose";
    public override int Health => 6;
    public override int Power => 5;

    public override IEnumerable<NecoUnitTag> Tags => new[] { NecoUnitTag.Carrier };

    public override IEnumerable<BaseBehavior> Actions => new[] {
        new ChaseBall(new[] { RelativeDirection.Up, RelativeDirection.UpLeft, RelativeDirection.UpRight }),
    };

    public override string BehaviorDescription
        =>
            $"If carrying the ball, moves [b]Direction[/b]. Otherwise, moves {Arrow7}, {Arrow0}, or {Arrow1}, whichever brings it closer to the ball.";
}
