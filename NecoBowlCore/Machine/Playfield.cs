using System.Diagnostics.CodeAnalysis;
using System.Text;
using NecoBowl.Core.Sport.Tactics;

namespace NecoBowl.Core.Machine;

internal readonly record struct NecoSpaceContents(Unit? Unit, bool Fooabr = false);

/// <summary>A two-dimensional grid of <see cref="NecoSpaceContents" />.</summary>
/// <remarks>
/// The <see cref="Unit" /> in a <see cref="NecoSpaceContents" /> is a reference to a mutable unit. Therefore, attempting
/// to copy a <c>Playfield</c> will result in that field having references to the units in the original field. If that
/// original field is then mutated in some way, properties like the unit's health will change on BOTH the original and copy
/// of the field. The contents of the copy are effectively garbage at that point. Basically, do not try to copy a field
/// without careful consideration!
/// </remarks>
internal class Playfield
{
    private readonly NecoSpaceContents[,] FieldContents;
    public readonly NecoFieldParameters FieldParameters;
    public readonly List<Unit> FlattenedMovementUnitBuffer = new();

    public readonly List<Unit> GraveyardZone = new();

    public Playfield(NecoFieldParameters param)
    {
        FieldParameters = param;
        FieldContents = new NecoSpaceContents[param.Bounds.X, param.Bounds.Y];
    }

    public NecoSpaceContents this[Vector2i p] {
        get => FieldContents[p.X, p.Y];
        set => FieldContents[p.X, p.Y] = value;
    }

    public NecoSpaceContents this[int x, int y] {
        get => this[new(x, y)];
        set => this[new(x, y)] = value;
    }

    public IEnumerable<NecoSpaceContents> Spaces
        => FieldContents.Cast<NecoSpaceContents>();

    public IEnumerable<(Vector2i, NecoSpaceContents)> SpacePositions {
        get {
            var spaces = Enumerable.Empty<(Vector2i, NecoSpaceContents)>();
            for (var y = 0; y < FieldContents.GetLength(1); y++)
            for (var x = 0; x < FieldContents.GetLength(0); x++) {
                spaces = spaces.Append((new(x, y), FieldContents[x, y]));
            }

            return spaces;
        }
    }

    public Unit? LookupUnit(string shortUid)
    {
        foreach (var unit in GetAllUnits(true).Select(tuple => tuple.Item2).Concat(GraveyardZone)) {
            if (unit.Id.Value.ToString().StartsWith(shortUid)) {
                return unit;
            }
        }

        return null;
    }


    public (int x, int y) GetBounds()
    {
        return (FieldContents.GetLength(0), FieldContents.GetLength(1));
    }

    private NecoSpaceContents FieldContentsByVec(Vector2i pos)
    {
        return FieldContents[pos.X, pos.Y];
    }

    public IEnumerable<(Vector2i, Unit)> GetAllUnits(bool includeInventory = false)
    {
        var spaceUnits = SpacePositions.Where(tuple => tuple.Item2.Unit is { })
            .Select(tuple => (tuple.Item1, tuple.Item2.Unit!)).ToList();
        var tempSpaceUnits = spaceUnits.ToList();
        if (includeInventory) {
            spaceUnits.AddRange(
                tempSpaceUnits.SelectMany(
                    tuple => tuple.Item2!.GetInventoryTree(false).Where(u => u != tuple.Item2)
                        .Select(u => (tuple.Item1, u))));
        }

        return spaceUnits;
    }

    public IReadOnlyList<Unit> GetGraveyard()
    {
        return GraveyardZone.AsReadOnly();
    }

    public bool TryLookupUnit(
        NecoUnitId uid,
        [NotNullWhen(true)] out Unit? unit,
        [NotNullWhen(true)] out Vector2i? pos,
        bool includeInventories = false)
    {
        foreach (var (p, u) in SpacePositions) {
            if (u.Unit is { }) {
                if (includeInventories) {
                    var match = u.Unit.GetInventoryTree().SingleOrDefault(u => u.Id == uid);
                    if (match is { }) {
                        unit = u.Unit;
                        pos = p;
                        return true;
                    }
                }
                else if (u.Unit.Id == uid) {
                    unit = u.Unit;
                    pos = p;
                    return true;
                }
            }
        }

        unit = null;
        pos = null;
        return false;
    }

    public Vector2i GetUnitPosition(NecoUnitId uid, bool searchInventory = false)
    {
        return SpacePositions.Single(
                tuple => tuple.Item2.Unit?.Id == uid
                    || (searchInventory
                        && (tuple.Item2.Unit?.Inventory.Any(inventoryUnit => inventoryUnit.Id == uid) ?? false)))
            .Item1;
    }

    public Unit GetUnit(NecoUnitId uid, bool includeInventories = true)
    {
        return GetUnit(uid, out _, includeInventories);
    }

    public Unit GetUnit(NecoUnitId uid, out Vector2i pos, bool includeInventories = false)
    {
        pos = GetUnitPosition(uid, includeInventories);
        return FieldContentsByVec(pos).Unit ?? throw new NecoBowlFieldException($"no unit found with ID {uid}");
    }

    public Unit GetUnit(Vector2i p)
    {
        return FieldContentsByVec(p).Unit ?? throw new NecoBowlFieldException($"no unit found at {p}");
    }

    public bool TryGetUnit(NecoUnitId uid, out Unit? unit)
    {
        return TryGetUnit(uid, out unit, out _);
    }

    public bool TryGetUnit(NecoUnitId uid, out Unit? unit, out Vector2i pos)
    {
        try {
            pos = GetUnitPosition(uid);
        }
        catch (InvalidOperationException) {
            unit = null;
            pos = default;
            return false;
        }

        unit = GetUnit(pos);
        return true;
    }

    public bool TryGetUnit(Vector2i p, out Unit? unit)
    {
        var spaceUnit = this[p].Unit;
        if (spaceUnit is { }) {
            unit = spaceUnit;
            return true;
        }

        unit = null;
        return false;
    }

    public Unit GetAndRemoveUnit(Vector2i p)
    {
        var unit = GetUnit(p);
        this[p] = this[p] with { Unit = null };
        return unit;
    }

    public Unit GetAndRemoveUnit(NecoUnitId uid)
    {
        return GetAndRemoveUnit(GetUnitPosition(uid));
    }

    public Unit GetAndRemoveUnit(NecoUnitId uid, out Vector2i pos)
    {
        pos = GetUnitPosition(uid);
        return GetAndRemoveUnit(pos);
    }

    public ReadOnlyPlayfield AsReadOnly()
    {
        return new(this);
    }

    public bool IsInBounds((int x, int y) pos)
    {
        return !(pos.x < 0 || pos.x >= GetBounds().x || pos.y < 0 || pos.y >= GetBounds().y);
    }

    public string ToAscii(string linePrefix = "> ")
    {
        var sb = new StringBuilder();

        void AddBorderH()
        {
            sb.Append("+");
            for (var i = 0; i < GetBounds().x; i++) {
                sb.Append("-");
            }

            sb.AppendLine("+");
        }

        var unitIcons = new Dictionary<Unit, char>();

        AddBorderH();
        for (var y = GetBounds().y - 1; y >= 0; y--) {
            sb.Append("|");
            for (var x = 0; x < GetBounds().x; x++) {
                var space = this[x, y];
                if (space.Unit is { }) {
                    unitIcons[space.Unit] = (char)('A' + unitIcons.Count);
                }

                sb.Append(space.Unit is null ? " " : unitIcons[space.Unit]);
            }

            sb.AppendLine("|");
        }

        AddBorderH();

        foreach (var (unit, icon) in unitIcons) {
            sb.AppendLine($"{icon}: {unit} ({unit.CurrentHealth} HP) ({unit.Inventory.Count} items)");
        }

        sb.Insert(0, linePrefix);
        sb.Replace("\n", $"\n{linePrefix}");

        return sb.ToString();
    }
}

public record class NecoFieldParameters((int X, int Y) Bounds, (int X, int Y) BallSpawnPoint, int TeamSideSize = 4)
{
    public NecoPlayerRole? GetPlayerAffiliation((int x, int y) pos)
    {
        return pos.y >= 0 && pos.y < TeamSideSize ? NecoPlayerRole.Offense
            : pos.y >= Bounds.Y - TeamSideSize && pos.y < Bounds.Y ? NecoPlayerRole.Defense
            : null;
    }
}

public class NecoBowlFieldException : Exception
{
    public NecoBowlFieldException()
    {
    }

    public NecoBowlFieldException(string message) : base(message)
    {
    }

    public NecoBowlFieldException(string message, Exception inner) : base(message, inner)
    {
    }
}
