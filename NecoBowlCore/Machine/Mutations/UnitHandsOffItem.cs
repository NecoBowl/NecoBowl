using NecoBowl.Core.Machine;

namespace NecoBowl.Core.Sport.Play;

public class UnitHandsOffItem : Mutation
{
    public readonly NecoUnitId Item;
    public readonly NecoUnitId Receiver;
    private Unit? TempUnitItem;

    public UnitHandsOffItem(NecoUnitId subject, NecoUnitId receiver, NecoUnitId item) : base(subject)
    {
        Receiver = receiver;
        Item = item;
    }

    public override string Description => $"{Subject} hands off {Item} to {Receiver}";

    internal override void Pass1Mutate(Playfield field)
    {
        var passer = field.GetUnit(Subject);
        var itemUnit = passer.Inventory.Single(u => u.Id == Item);
        passer.Inventory.Remove(itemUnit);
        TempUnitItem = itemUnit;
        TempUnitItem.Carrier = null;
    }

    internal override void Pass3Mutate(Playfield field)
    {
        var receiver = field.GetUnit(Receiver);
        receiver.Inventory.Add(TempUnitItem!);
        TempUnitItem!.Carrier = receiver;
    }
}
