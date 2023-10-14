using NecoBowl.Core.Machine;

namespace NecoBowl.Core.Sport.Play;

public class UnitThrowsItem : BaseMutation
{
    public readonly Vector2i Destination;
    public readonly NecoUnitId Item;

    public UnitThrowsItem(NecoUnitId subject, NecoUnitId item, Vector2i destination) : base(subject)
    {
        Item = item;
        Destination = destination;
    }

    public override string Description => $"{Subject} throws {Item} to {Destination}";

    internal override void Pass3Mutate(Playfield field)
    {
        // TODO Sanity check and make sure the item is in the Subject's inventory
        var itemUnit = field.GetUnit(Item);
        var subject = field.GetUnit(Subject);
        itemUnit.Carrier = null;
        subject.Inventory.Remove(itemUnit);

        var unitAtPosition = field.GetAllUnits(true)
            .Single(t => t.Item1 == Destination && t.Item2.HandoffItem() is null).Item2;
        unitAtPosition.Inventory.Add(itemUnit);
        itemUnit.Carrier = unitAtPosition;

        // carrier is adding itself
    }
}
