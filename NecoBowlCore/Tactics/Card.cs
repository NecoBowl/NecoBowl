using System.Collections;
using NecoBowl.Core.Machine;
using NecoBowl.Core.Model;
using NecoBowl.Core.Sport.Tactics;
using NecoBowl.Core.Tags;

namespace NecoBowl.Core.Tactics;

/// <summary>
/// A game object that can be placed on the field by a player. Typically, a selection of these are presented to players in
/// a "hand" of cards.
/// </summary>
public abstract class Card
{
    public readonly CardModel? CardModel;
    public readonly int Cost;
    public readonly NecoCardOptions Options;

    public Card(CardModel? cardModel)
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
}

public class UnitCard : Card
{
    public UnitCard(UnitCardModel? cardModel) : base(cardModel)
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

public class NecoCardOptions : ICollection<NecoCardOptionItem>
{
    private readonly Dictionary<string, object> Values;

    public NecoCardOptions()
    {
        Values = new();
    }

    public NecoCardOptions(CardModel? cardModel)
    {
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

    public void Add(NecoCardOptionItem item)
    {
        Values[item.OptionIdentifier] = item.OptionValue;
    }

    public void Clear()
    {
        Values.Clear();
    }

    public bool Contains(NecoCardOptionItem item)
    {
        return Values.TryGetValue(item.OptionIdentifier, out var itemValue) && item.Equals(itemValue);
    }

    public void CopyTo(NecoCardOptionItem[] array, int arrayIndex)
    {
        this.AsEnumerable().ToList().CopyTo(array);
    }

    public bool Remove(NecoCardOptionItem item)
    {
        if (Contains(item)) {
            Values.Remove(item.OptionIdentifier);
            return true;
        }

        return false;
    }

    public int Count => Values.Count;
    public bool IsReadOnly => false;
}
