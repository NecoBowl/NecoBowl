using System.Collections;
using NecoBowl.Core.Machine;
using NecoBowl.Core.Model;
using NecoBowl.Core.Tags;

namespace NecoBowl.Core.Sport.Tactics;

public class Card
{
    public readonly CardModel CardModel;
    public readonly NecoCardOptions Options;
    public int Cost;

    public Card(CardModel cardModel)
    {
        CardModel = cardModel;
        Cost = CardModel.Cost;
        Options = new(CardModel);
    }

    public string Name => CardModel.Name;

    public bool IsUnitCard()
    {
        return CardModel is UnitCardModel;
    }

    public bool IsUnitCard(out UnitCard? unitCard)
    {
        if (IsUnitCard()) {
            unitCard = (UnitCard)this;
            return true;
        }

        unitCard = null;
        return false;
    }
}

public class UnitCard : Card
{
    public UnitCard(UnitCardModel cardModel) : base(cardModel)
    {
    }

    public UnitModel UnitModel
        => ((UnitCardModel)CardModel).Model;

    internal Unit ToUnit(NecoPlayerId playerId)
    {
        var unit = new Unit(UnitModel, playerId);
        foreach (var planOption in CardModel.OptionPermissions) {
            planOption.ApplyToUnit(unit, Options[planOption.Identifier]);
        }

        return unit;
    }
}

public class NecoCardOptions : IEnumerable<NecoCardOptionItem>
{
    private readonly CardModel CardModel;
    private readonly Dictionary<string, object> Values;

    public NecoCardOptions(CardModel cardModel)
    {
        CardModel = cardModel;
        Values = cardModel.OptionPermissions.ToDictionary(p => p.Identifier, p => p.Default);
    }

    public object this[string id] => Values[id];

    public IEnumerator<NecoCardOptionItem> GetEnumerator()
    {
        return Values.AsEnumerable().Select(kv => new NecoCardOptionItem(kv.Key, kv.Value)).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public object? GetValue(string id)
    {
        return Values.TryGetValue(id, out var value) ? value : null;
    }

    public void SetValue(string id, object value)
    {
        if (!Values.ContainsKey(id)) {
            throw new CardOptionException($"invalid key {id}");
        }

        if (value.GetType() != Values[id].GetType()) {
            throw new CardOptionException(
                $"invalid type for option {id} (was {value.GetType()}, expected {Values[id].GetType()}");
        }

        Values[id] = value;
    }
}
