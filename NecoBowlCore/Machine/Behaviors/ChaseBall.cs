using NecoBowl.Core.Machine;
using NecoBowl.Core.Tags;

namespace NecoBowl.Core.Sport.Play;

public class ChaseBall : Behavior
{
    public const string Option_FallbackDirecttion = "FallbackDirection";
    private readonly RelativeDirection[] AllowedDirections;
    private readonly RelativeDirection FallbackDirection;

    public ChaseBall(RelativeDirection[] allowedDirections, RelativeDirection? fallback = null)
    {
        if (allowedDirections.Length == 0) {
            throw new NecoUnitActionException("no directions provided");
        }

        FallbackDirection = fallback ?? allowedDirections[0];
        AllowedDirections = allowedDirections;
    }

    protected override BehaviorOutcome CallResult(NecoUnitId uid, ReadOnlyPlayfield field)
    {
        var unit = field.GetUnit(uid, out var pos);
        var (ballPos, ball)
            = field.GetAllUnits().SingleOrDefault(tup => tup.Item2.Tags.Contains(NecoUnitTag.TheBall));
        var (minDistanceDirection, minDistanceAfterMove) = AllowedDirections.Select(
                dir => {
                    var lengthSquared = (pos + dir.ToVector2i(unit.Facing) - ballPos).LengthSquared;
                    return (dir, lengthSquared);
                })
            .MinBy(tuple => tuple.lengthSquared);
        var originalDistance = (pos - ballPos).LengthSquared;

        var fallbackDirection = (RelativeDirection)(unit.GetMod<NecoUnitMod.OptionValues>()
            .GetValueOrNull<RelativeDirection>(Option_FallbackDirecttion) ?? FallbackDirection);
        var direction = minDistanceAfterMove > originalDistance ? fallbackDirection : minDistanceDirection;
        return new TranslateUnit(direction).CallResult(uid, field);
    }
}
