using System.Collections;
using System.Text.Json.Serialization;
using NecoBowl.Core.Machine;
using NecoBowl.Core.Model;
using NecoBowl.Core.Sport.Tactics;
using NecoBowl.Core.Tags;
using NLog;

namespace NecoBowl.Core.Tactics;

public record NecoCardId
{
    public const int StringLength = 6;

    public readonly Guid Value = Guid.NewGuid();

    internal NecoCardId()
    {
    }

    [JsonConstructor]
    public NecoCardId(Guid value)
    {
        Value = value;
    }

    public string ToSimpleString()
    {
        return Value.ToString().Substring(0, StringLength);
    }

    public override string ToString()
    {
        return $"@U:{Value.ToString().Substring(0, StringLength)}";
    }
}

/// <summary>
/// A game object that can be placed on the field by a player. Typically, a selection of these are presented to players in
/// a "hand" of cards.
/// </summary>
public abstract class Card
{
    public readonly CardModel? CardModel;
    public readonly int Cost;
    public readonly NecoCardOptions Options;
    public readonly NecoCardId CardId;

    public Card(CardModel? cardModel, Guid? id = null)
    {
        id ??= Guid.NewGuid();
        
        CardModel = cardModel;
        Cost = CardModel.Cost;
        Options = new(CardModel);
        CardId = new(id.Value);
    }

    public string Name => CardModel.Name;

    public bool IsUnitCard()
    {
        return CardModel is UnitCardModel;
    }
}

public class UnitCard : Card
{
    public UnitCard(UnitCardModel? cardModel, Guid? id = null) : base(cardModel, id)
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

        if (Values[id] is Enum && value is string) {
            // Stupid hack for when enum values get sent as strings.
            value = Enum.Parse(Values[id].GetType(), (string)value);
        } else if (value.GetType() != Values[id].GetType()) {
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
