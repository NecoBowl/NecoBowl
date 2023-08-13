using System.Collections;

using neco_soft.NecoBowlCore.Action;
using neco_soft.NecoBowlCore.Model;
using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlCore.Tactics;

public class NecoCard
{
    public readonly NecoCardModel CardModel;
    public readonly NecoCardOptions Options;
    public int Cost;

    public NecoCard(NecoCardModel cardModel)
    {
        CardModel = cardModel;
        Cost = CardModel.Cost;
        Options = new(CardModel);
    }

    public string Name => CardModel.Name;

    public bool IsUnitCard()
    {
        return CardModel is NecoUnitCardModel;
    }

    public bool IsUnitCard(out NecoUnitCard? unitCard)
    {
        if (IsUnitCard()) {
            unitCard = (NecoUnitCard)this;
            return true;
        }

        unitCard = null;
        return false;
    }
}

public class NecoUnitCard : NecoCard
{
    public NecoUnitCard(NecoUnitCardModel cardModel) : base(cardModel)
    { }

    public NecoUnitModel UnitModel
        => ((NecoUnitCardModel)CardModel).Model;

    public NecoUnit ToUnit(NecoPlayerId playerId)
    {
        var unit = new NecoUnit(UnitModel, playerId);
        foreach (var planOption in CardModel.OptionPermissions)
            planOption.ApplyToUnit(unit, Options[planOption.Identifier]);

        return unit;
    }
}

public class NecoCardOptions : IEnumerable<(string, object)>
{
    private readonly NecoCardModel CardModel;
    private readonly Dictionary<string, object> Values;

    public NecoCardOptions(NecoCardModel cardModel)
    {
        CardModel = cardModel;
        Values = cardModel.OptionPermissions.ToDictionary(p => p.Identifier, p => p.Default);
    }

    public object this[string id] => Values[id];

    public IEnumerator<(string, object)> GetEnumerator()
    {
        return Values.AsEnumerable().Select(kv => (kv.Key, kv.Value)).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void SetValue(string id, object value)
    {
        if (!Values.ContainsKey(id)) throw new CardOptionException($"invalid key {id}");

        if (value.GetType() != Values[id].GetType())
            throw new CardOptionException(
                $"invalid type for option {id} (was {value.GetType()}, expected {Values[id].GetType()}");

        Values[id] = value;
    }
}