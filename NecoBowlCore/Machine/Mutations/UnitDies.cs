using NecoBowl.Core.Machine;
using Unit = NecoBowl.Core.Reports.Unit;

namespace NecoBowl.Core.Sport.Play;

public class UnitDies : BaseMutation
{
    internal UnitDies(Unit subject)
        : base(subject.Id)
    {
    }

    public override string Description => $"{Subject} is destroyed";

    internal override NecoUnitId[] ExtractedUnits => new[] { Subject };

    internal override bool Prepare(
        IPlayfieldChangeReceiver context, ReadOnlyPlayfield field)
    {
        if (context.MutationIsBuffered<UnitDies>(Subject) is { } existing && existing != this) {
            return true;
        }

        return false;
    }

    internal override void Pass1Mutate(Playfield field)
    {
        var unit = field.GetAndRemoveUnit(Subject);
        field.GraveyardZone.Add(unit);
    }

    internal override void Pass2Mutate(Playfield field)
    {
    }
}
