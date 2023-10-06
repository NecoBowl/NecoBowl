using NecoBowl.Core.Machine;
using NecoBowl.Core.Tags;

namespace NecoBowl.Core.Sport.Play;

public class UnitGetsMod : Mutation
{
    public readonly UnitMod Mod;

    public UnitGetsMod(NecoUnitId subject, UnitMod mod)
        : base(subject)
    {
        Mod = mod;
    }

    public override string Description => $"{Subject} gets {Mod}";

    internal override void Pass1Mutate(Playfield field)
    {
        var unit = field.GetUnit(Subject);
        unit.AddMod(Mod);
    }

    internal override void Pass2Mutate(Playfield field)
    {
    }
}
