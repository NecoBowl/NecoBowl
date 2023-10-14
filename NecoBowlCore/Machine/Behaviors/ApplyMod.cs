using NecoBowl.Core.Machine.Mutations;
using NecoBowl.Core.Tags;

namespace NecoBowl.Core.Machine.Behaviors;

public class ApplyMod : BaseBehavior
{
    public readonly UnitMod Mod;

    public ApplyMod(UnitMod mod)
    {
        Mod = mod;
    }

    internal override BehaviorOutcome CallResult(NecoUnitId uid, ReadOnlyPlayfield field)
    {
        return BehaviorOutcome.Success(new UnitGetsMod(field.GetUnit(uid).ToReport(), Mod));
    }
}
