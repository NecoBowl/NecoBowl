using System.Text;

namespace neco_soft.NecoBowlCore.Action;

public readonly record struct NecoSpaceContents(NecoUnit? Unit, bool Fooabr = false);

/// <summary>
/// A two-dimensional grid of <see cref="NecoSpaceContents"/>.
/// </summary>
public class NecoField
{
    protected readonly NecoSpaceContents[,] FieldContents;

    public NecoField(NecoFieldParameters param)
    {
        FieldContents = new NecoSpaceContents[param.Bounds.X, param.Bounds.Y];
    }
    
    public NecoField(uint width, uint height)
    {
        FieldContents = new NecoSpaceContents[width, height];
    }

    public NecoField(NecoField field, bool deep = false)
    {
        FieldContents = (NecoSpaceContents[,])field.FieldContents.Clone();

        if (deep) {
            foreach (var u in GetAllUnits()) {
                throw new NotImplementedException("cannot clone fields with units");
            }
        }
    }

    public NecoSpaceContents this[Vector2i p] {
        get => FieldContents[p.X, p.Y];
        set => FieldContents[p.X, p.Y] = value;
    }

    public NecoSpaceContents this[int x, int y] {
        get => this[new Vector2i(x, y)];
        set => this[new Vector2i(x, y)] = value;
    }
    
    public (int x, int y) GetBounds()
        => (FieldContents.GetLength(0), FieldContents.GetLength(1));

    public IEnumerable<NecoSpaceContents> Spaces
        => FieldContents.Cast<NecoSpaceContents>();

    private NecoSpaceContents FieldContentsByVec(Vector2i pos)
        => FieldContents[pos.X, pos.Y];

    public IEnumerable<(Vector2i, NecoSpaceContents)> SpacePositions {
        get {
            var spaces = Enumerable.Empty<(Vector2i, NecoSpaceContents)>();
            for (var y = 0; y < FieldContents.GetLength(1); y++) {
                for (var x = 0; x < FieldContents.GetLength(0); x++) {
                    spaces = spaces.Append((new(x, y), FieldContents[x,y]));
                }
            }

            return spaces;
        }
    }

    public IEnumerable<(Vector2i, NecoUnit)> GetAllUnits()
        => SpacePositions.Where(tuple => tuple.Item2.Unit is not null).Select(tuple => (tuple.Item1, tuple.Item2.Unit!));

    public Vector2i GetUnitPosition(NecoUnitId uid)
        => SpacePositions.Single(tuple => tuple.Item2.Unit?.Id == uid).Item1;

    public NecoUnit GetUnit(NecoUnitId uid)
        => FieldContentsByVec(GetUnitPosition(uid)).Unit ?? throw new NecoBowlFieldException($"no unit found with ID {uid}");

    public NecoUnit GetUnit(Vector2i p)
            => FieldContentsByVec(p).Unit ?? throw new NecoBowlFieldException($"no unit found at {p}");

    public bool TryGetUnit(NecoUnitId uid, out NecoUnit? unit)
    {
        Vector2i pos;
        try {
            pos = GetUnitPosition(uid);
        } catch (InvalidOperationException) {
            unit = null;
            return false;
        }

        unit = GetUnit(pos);
        return true;
    }

    public bool TryGetUnit(Vector2i p, out NecoUnit? unit)
    {
        var spaceUnit = this[p].Unit;
        if (spaceUnit is not null) {
            unit = spaceUnit;
            return true;
        }
        unit = null;
        return false;
    }

    public NecoUnit GetAndRemoveUnit(Vector2i p)
    {
        var unit = GetUnit(p);
        this[p] = this[p] with { Unit = null };
        return unit;
    }

    public string ToAscii(string linePrefix = "> ")
    {
        var sb = new StringBuilder();

        void AddBorderH()
        {
            sb.Append("+");
            for (int i = 0; i < GetBounds().x; i++) {
                sb.Append("-");
            }

            sb.AppendLine("+");
        }
        
        var unitIcons = new Dictionary<NecoUnit, char>();

        AddBorderH();
        for (int y = GetBounds().y - 1; y >= 0; y--) {
            sb.Append("|");
            for (int x = 0; x < GetBounds().x; x++) {
                var space = this[x, y];
                if (space.Unit is not null) {
                    unitIcons[space.Unit] = (char)('A' + unitIcons.Count);
                }

                sb.Append(space.Unit is null ? " " : unitIcons[space.Unit]);
            }

            sb.AppendLine("|");
        }

        AddBorderH();

        foreach (var (unit, icon) in unitIcons) {
            sb.AppendLine($"{icon}: {unit}");
        }

        sb.Insert(0, linePrefix);
        sb.Replace("\n", $"\n{linePrefix}");

        return sb.ToString();
    }
}

public record class NecoFieldParameters((int X, int Y) Bounds);

/// <summary>
/// Wrapper around a <see cref="NecoField"/> that prevents modifications to the field.
/// </summary>
public sealed class ReadOnlyNecoField
{
    private readonly NecoField Field;

    public ReadOnlyNecoField(NecoField field)
    {
        Field = field;
    }

    public NecoSpaceContents this[int x, int y] => Field[x, y];
    public NecoSpaceContents this[Vector2i pos] => Field[pos.X, pos.Y];

    public static implicit operator ReadOnlyNecoField(NecoField field)
        => new ReadOnlyNecoField(field);

    public IEnumerable<(Vector2i, NecoUnit)> GetAllUnits()
        => Field.GetAllUnits();

    public Vector2i GetUnitPosition(NecoUnitId uid)
        => Field.GetUnitPosition(uid);

    public NecoUnit GetUnit(NecoUnitId uid)
        => Field.GetUnit(uid);

    public NecoUnit GetUnit(Vector2i p)
        => Field.GetUnit(p);

    public bool TryGetUnit(NecoUnitId uid, out NecoUnit? unit)
        => Field.TryGetUnit(uid, out unit);

    public bool TryGetUnit(Vector2i p, out NecoUnit? unit)
        => Field.TryGetUnit(p, out unit);

    public Vector2i GetBounds()
        => Field.GetBounds();
}

public class NecoBowlFieldException : Exception
{
    public NecoBowlFieldException() { }
    public NecoBowlFieldException(string message) : base(message) { }
    public NecoBowlFieldException(string message, Exception inner) : base(message, inner) { }
}
