using NecoBowl.Core.Machine;

namespace NecoBowl.Core.Sport.Play;

public class UnitPicksUpItem : Mutation
{
    public readonly NecoUnitId Item;

    public UnitPicksUpItem(NecoUnitId subject, NecoUnitId item)
        : base(subject)
    {
        Item = item;
    }

    public override string Description => $"{Subject} picks up {Item}";

    internal override NecoUnitId[] ExtractedUnits => new[] { Item };

    internal override bool Prepare(NecoSubstepContext context, ReadOnlyPlayfield field)
    {
        if (field.GetUnit(Subject).Carrier is not null) {
            throw new NecoBowlException("a unit with an inventory cannot be picked up");
        }

        return false;
    }

    internal override void Pass1Mutate(Playfield field)
    {
        var itemUnit = field.TempUnitZone.Single(u => u.Id == Item);
    }

    internal override void Pass3Mutate(Playfield field)
    {
        var itemUnit = field.TempUnitZone.Single(u => u.Id == Item);
        var subject = field.GetUnit(Subject);
        itemUnit!.Carrier = subject;
        subject.Inventory.Add(itemUnit!);
    }
}
