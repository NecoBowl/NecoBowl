namespace NecoBowl.Core.Machine.Reports;

public class Playfield : BaseReport
{
    private readonly ReadOnlyPlayfield Field;

    internal NecoFieldInformation(ReadOnlyPlayfield field)
    {
        Field = field;
    }

    public NecoSpaceInformation this[int x, int y] => this[(x, y)];
    public NecoSpaceInformation this[(int, int) coords] => Contents(coords);

    public NecoFieldParameters FieldParameters => Field.FieldParameters;

    public NecoUnitInformation GetUnit(NecoUnitId uid)
    {
        return new(Field.GetUnit(uid));
    }

    public NecoUnitInformation? LookupUnit(string shortUid)
    {
        var unit = Field.LookupUnit(shortUid);
        return unit is null ? null : new(unit);
    }

    public Vector2i GetUnitPosition(NecoUnitId uid, bool includeInventories = false)
    {
        return Field.GetUnitPosition(uid, includeInventories);
    }

    public IReadOnlyList<Unit> GetGraveyard()
    {
        return Field.GetGraveyard();
    }

    public NecoSpaceInformation Contents((int, int) coords)
    {
        return new(Field[coords], coords, Field.FieldParameters.GetPlayerAffiliation(coords));
    }

    public (int x, int y) GetBounds()
    {
        return Field.GetBounds();
    }
}
