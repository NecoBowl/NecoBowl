using NecoBowl.Core.Machine;
using NecoBowl.Core.Tags;

namespace NecoBowl.Core.Sport.Play;

public class ApplyMod : Behavior
{
    public readonly UnitMod Mod;

    public ApplyMod(UnitMod mod)
    {
        Mod = mod;
    }

    internal override BehaviorOutcome CallResult(NecoUnitId uid, ReadOnlyPlayfield field)
    {
        return BehaviorOutcome.Success(new UnitGetsMod(uid, Mod));
    }
}
