using NecoBowl.Core.Machine;
using Unit = NecoBowl.Core.Reports.Unit;

namespace NecoBowl.Core.Sport.Play;

public class UnitPicksUpItem : BaseMutation
{
    public readonly Unit Item;

    public UnitPicksUpItem(Unit subject, Unit item)
        : base(subject.Id)
    {
        Item = item;
    }

    public override string Description => $"{Subject} picks up {Item}";

    internal override NecoUnitId[] ExtractedUnits => new[] { Item.Id };

    internal override bool Prepare(NecoSubstepContext context, ReadOnlyPlayfield field)
    {
        if (field.GetUnit(Subject).Carrier is { }) {
            throw new NecoBowlException("a unit with an inventory cannot be picked up");
        }

        return false;
    }

    internal override void Pass3Mutate(Playfield field)
    {
        var itemUnit = field.FlattenedMovementUnitBuffer.Single(u => u.Id == Item.Id);
        var subject = field.GetUnit(Subject);
        itemUnit!.Carrier = subject;
        subject.Inventory.Add(itemUnit!);
    }
}
