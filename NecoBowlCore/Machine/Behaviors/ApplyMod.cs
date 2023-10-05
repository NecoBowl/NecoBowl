using NecoBowl.Core.Machine;
using NecoBowl.Core.Tags;

namespace NecoBowl.Core.Sport.Play;

public class ApplyMod : Behavior
{
    public readonly NecoUnitMod Mod;

    public ApplyMod(NecoUnitMod mod)
    {
        Mod = mod;
    }

    internal override BehaviorOutcome CallResult(NecoUnitId uid, ReadOnlyPlayfield field)
    {
        return BehaviorOutcome.Success(new UnitGetsMod(uid, Mod));
    }
}
