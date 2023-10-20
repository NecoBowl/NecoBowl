using NecoBowl.Core.Machine;

namespace NecoBowl.Core.Reports;

// TODO Make this fully immutable
public record Playfield : BaseReport
{
    private readonly ReadOnlyPlayfield Field;

    internal Playfield(ReadOnlyPlayfield field)
    {
        Field = field;
    }

    public Space this[int x, int y] => this[(x, y)];
    public Space this[(int, int) coords] => Contents(coords);

    public NecoFieldParameters FieldParameters => Field.FieldParameters;

    public Unit GetUnit(NecoUnitId uid)
    {
        return new(Field.GetUnit(uid));
    }

    public Unit? LookupUnit(string shortUid)
    {
        var unit = Field.LookupUnit(shortUid);
        return unit is null ? null : new(unit);
    }

    public Vector2i GetUnitPosition(NecoUnitId uid, bool includeInventories = false)
    {
        return Field.GetUnitPosition(uid, includeInventories);
    }

    public IReadOnlyCollection<Unit> GetGraveyard()
    {
        return Field.GetGraveyard().Select(m => new Unit(m)).ToList().AsReadOnly();
    }

    public Space Contents(Vector2i coords)
    {
        var unit = Field[coords.X, coords.Y].Unit;
        return new(unit is null ? null : new Unit(unit));
    }

    public (int x, int y) GetBounds()
    {
        return Field.GetBounds();
    }
}
