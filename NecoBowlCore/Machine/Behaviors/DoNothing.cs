using NecoBowl.Core.Machine;

namespace NecoBowl.Core.Sport.Play;

public class DoNothing : Behavior
{
    internal override BehaviorOutcome CallResult(NecoUnitId uid, ReadOnlyPlayfield field)
    {
        return new BehaviorOutcome.Nothing();
    }
}
