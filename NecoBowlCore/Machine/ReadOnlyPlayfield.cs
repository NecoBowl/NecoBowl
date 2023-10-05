using NecoBowl.Core.Sport.Play;

namespace NecoBowl.Core.Machine;

/// <summary>Wrapper around a <see cref="Playfield" /> that prevents modifications to the field.</summary>
/// <remarks>
/// Note that this field is not immutable; changes made to the field from which the read-only field is derived will still
/// appear when reading from the read-only field.
/// </remarks>
public sealed class ReadOnlyPlayfield
{
    private readonly Playfield Field;

    internal ReadOnlyPlayfield(Playfield field)
    {
        Field = field;
    }

    #region Wrapped methods - Field

    public NecoSpaceContents this[int x, int y] => Field[x, y];
    public NecoSpaceContents this[Vector2i pos] => Field[pos];

    public NecoFieldParameters FieldParameters => Field.FieldParameters;

    public IEnumerable<(Vector2i, Unit)> GetAllUnits(bool includeInventory = false)
    {
        return Field.GetAllUnits(includeInventory);
    }

    public IReadOnlyList<Unit> GetGraveyard()
    {
        return Field.GetGraveyard();
    }

    public Unit? LookupUnit(string shortUid)
    {
        return Field.LookupUnit(shortUid);
    }

    public Vector2i GetUnitPosition(NecoUnitId uid, bool includeInventories = false)
    {
        return Field.GetUnitPosition(uid, includeInventories);
    }

    public Unit GetUnit(NecoUnitId uid)
    {
        return Field.GetUnit(uid);
    }

    public Unit GetUnit(Vector2i p)
    {
        return Field.GetUnit(p);
    }

    public Unit GetUnit(NecoUnitId uid, out Vector2i pos)
    {
        return Field.GetUnit(uid, out pos);
    }

    public bool TryGetUnit(NecoUnitId uid, out Unit? unit)
    {
        return Field.TryGetUnit(uid, out unit);
    }

    public bool TryGetUnit(Vector2i p, out Unit? unit)
    {
        return Field.TryGetUnit(p, out unit);
    }

    public Vector2i GetBounds()
    {
        return Field.GetBounds();
    }

    public bool IsInBounds((int x, int y) pos)
    {
        return Field.IsInBounds(pos);
    }

    public string ToAscii(string prefix = "> ")
    {
        return Field.ToAscii(prefix);
    }

    public bool TryLookupUnit(NecoUnitId uid, out Unit? unit, out Vector2i? pos, bool includeInventories = false)
    {
        return Field.TryLookupUnit(uid, out unit, out pos, includeInventories);
    }

    #endregion
}
