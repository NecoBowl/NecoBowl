using NecoBowl.Core.Tags;

namespace NecoBowl.Core.Machine.Mutations;

public class UnitGetsMod : BaseMutation
{
    public readonly UnitMod Mod;

    public UnitGetsMod(Core.Reports.Unit subject, UnitMod mod)
        : base(subject.Id)
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
