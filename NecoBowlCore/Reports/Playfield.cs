namespace NecoBowl.Core.Machine.Reports;

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

    public Core.Reports.Unit GetUnit(NecoUnitId uid)
    {
        return new(Field.GetUnit(uid));
    }

    public Core.Reports.Unit? LookupUnit(string shortUid)
    {
        var unit = Field.LookupUnit(shortUid);
        return unit is null ? null : new(unit);
    }

    public Vector2i GetUnitPosition(NecoUnitId uid, bool includeInventories = false)
    {
        return Field.GetUnitPosition(uid, includeInventories);
    }

    public IReadOnlyCollection<Core.Reports.Unit> GetGraveyard()
    {
        return Field.GetGraveyard().Select(m => new Core.Reports.Unit(m)).ToList().AsReadOnly();
    }

    public Space Contents(Vector2i coords)
    {
        var unit = Field[coords.X, coords.Y].Unit;
        return new(unit is null ? null : new Core.Reports.Unit(unit));
    }

    public (int x, int y) GetBounds()
    {
        return Field.GetBounds();
    }
}
