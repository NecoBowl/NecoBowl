using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Newtonsoft.Json.Converters;

[assembly: InternalsVisibleTo("NecoBowlTest")]


namespace NecoBowl.Core;

public enum AbsoluteDirection : int
{
    North = 0,
    NorthEast = 1,
    East = 2,
    SouthEast = 3,
    South = 4,
    SouthWest = 5,
    West = 6,
    NorthWest = 7
}

public enum RelativeDirection : int
{
    Up,
    UpRight,
    Right,
    DownRight,
    Down,
    DownLeft,
    Left,
    UpLeft
}

#pragma warning disable format
// ReSharper disable once InconsistentNaming
public readonly record struct Vector2i(int X, int Y)
{
    public static readonly Vector2i Zero = new(0, 0);
    public static readonly Vector2i Up = new(0, 1);
    public static readonly Vector2i Right = new(1, 0);
    public static readonly Vector2i Down = new(0, -1);
    public static readonly Vector2i Left = new(-1, 0);
    public int LengthSquared => (X * X) + (Y * Y);
    public int Area => X * Y;

    public static Vector2i operator +(Vector2i left, Vector2i right)
    {
        return new(left.X + right.X, left.Y + right.Y);
    }

    public static Vector2i operator +(Vector2i left, int right)
    {
        return new(left.X + right, left.Y + right);
    }

    public static Vector2i operator -(Vector2i left, Vector2i right)
    {
        return new(left.X - right.X, left.Y - right.Y);
    }

    public static Vector2i operator -(Vector2i left, int right)
    {
        return new(left.X - right, left.Y - right);
    }

    public static Vector2i operator *(Vector2i left, Vector2i right)
    {
        return new(left.X * right.X, left.Y * right.Y);
    }

    public static Vector2i operator *(Vector2i left, int right)
    {
        return new(left.X * right, left.Y * right);
    }

    public static Vector2i operator /(Vector2i left, Vector2i right)
    {
        return new(left.X / right.X, left.Y / right.Y);
    }

    public static Vector2i operator /(Vector2i left, int right)
    {
        return new(left.X / right, left.Y / right);
    }

    public static implicit operator (int, int)(Vector2i input)
    {
        return (input.X, input.Y);
    }

    public static implicit operator Vector2i((int, int) input)
    {
        return new(input.Item1, input.Item2);
    }

    public override string ToString()
    {
        return $"({X}, {Y})";
    }
}
#pragma warning restore format

public static class AbsoluteDirectionExt
{
    public static AbsoluteDirection Opposite(this AbsoluteDirection direction)
    {
        return direction.RotatedBy(4);
    }

    public static AbsoluteDirection RotatedBy(this AbsoluteDirection direction, RelativeDirection rel)
    {
        return RotatedBy(direction, (int)rel);
    }

    public static AbsoluteDirection RotatedBy(this AbsoluteDirection direction, int rotation)
    {
        return (AbsoluteDirection)(((uint)direction + rotation) % 8);
    }

    // ReSharper disable once InconsistentNaming
    public static Vector2i ToVector2i(this AbsoluteDirection direction)
    {
        return direction switch {
            AbsoluteDirection.North => Vector2i.Up,
            AbsoluteDirection.NorthEast => Vector2i.Up + Vector2i.Right,
            AbsoluteDirection.East => Vector2i.Right,
            AbsoluteDirection.SouthEast => Vector2i.Down + Vector2i.Right,
            AbsoluteDirection.South => Vector2i.Down,
            AbsoluteDirection.SouthWest => Vector2i.Down + Vector2i.Left,
            AbsoluteDirection.West => Vector2i.Left,
            AbsoluteDirection.NorthWest => Vector2i.Up + Vector2i.Left,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };
    }

    // ReSharper disable once InconsistentNaming
    public static Vector2i ToVector2i(this RelativeDirection direction, AbsoluteDirection facing)
    {
        return facing.RotatedBy(direction).ToVector2i();
    }

    public static AbsoluteDirection Mirror(this AbsoluteDirection direction, bool mirrorX, bool mirrorY)
    {
        var vec = direction.ToVector2i();
        var newVec = new Vector2i(mirrorX ? -vec.X : vec.X, mirrorY ? -vec.Y : vec.Y);
        try {
            return Enum.GetValues<AbsoluteDirection>().Single(d => d.ToVector2i() == newVec);
        }
        catch (InvalidOperationException e) {
            throw new NecoBowlException("Failed to find mirrored vector", e);
        }
    }
}

public static class RelativeDirectionExt
{
    public static readonly int[] RelativeDirectionToArrowGlyph = {
        1, // Up
        7, // UpRight
        2, // Right
        8, // DownRight
        3, // Down
        9, // DownLeft
        0, // Left
        6 // UpLeft
    };

    // ReSharper disable once InconsistentNaming
    public static Vector2i ToVector2i(this RelativeDirection direction)
    {
        return direction switch {
            RelativeDirection.Up => Vector2i.Up,
            RelativeDirection.UpRight => Vector2i.Up + Vector2i.Right,
            RelativeDirection.Right => Vector2i.Right,
            RelativeDirection.DownRight => Vector2i.Down + Vector2i.Right,
            RelativeDirection.Down => Vector2i.Down,
            RelativeDirection.DownLeft => Vector2i.Down + Vector2i.Left,
            RelativeDirection.Left => Vector2i.Left,
            RelativeDirection.UpLeft => Vector2i.Up + Vector2i.Left,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };
    }

    public static string ToArrowGlyph(this RelativeDirection direction)
    {
        // 🡨 
        const int arrowCodepointOffset = 0x1f868;

        return char.ConvertFromUtf32(
            arrowCodepointOffset +
            direction switch {
                RelativeDirection.Up => 1,
                RelativeDirection.UpRight => 5,
                RelativeDirection.Right => 2,
                RelativeDirection.DownRight => 6,
                RelativeDirection.Down => 3,
                RelativeDirection.DownLeft => 7,
                RelativeDirection.Left => 0,
                RelativeDirection.UpLeft => 4,
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
            });
    }

    public static RelativeDirection RotatedBy(this RelativeDirection direction, int rotation)
    {
        return (RelativeDirection)(((uint)direction + rotation) % 8);
    }

    public static RelativeDirection Mirror(this RelativeDirection direction, bool mirrorX, bool mirrorY)
    {
        var vec = direction.ToVector2i();
        var newVec = new Vector2i(mirrorX ? -vec.X : vec.X, mirrorY ? -vec.Y : vec.Y);
        try {
            return Enum.GetValues<RelativeDirection>().Single(d => d.ToVector2i() == newVec);
        }
        catch (InvalidOperationException e) {
            throw new NecoBowlException("Failed to find mirrored vector", e);
        }
    }
}

public static class Ext
{
    /// <summary>Gets an attribute on an enum field value</summary>
    /// <typeparam name="T">The type of the attribute you want to retrieve</typeparam>
    /// <param name="enumVal">The enum value</param>
    /// <returns>The attribute of type T that exists on the enum value</returns>
    public static T? GetAttributeOfType<T>(this Enum enumVal) where T : Attribute
    {
        var type = enumVal.GetType();
        var memInfo = type.GetMember(enumVal.ToString());
        var attributes = memInfo[0].GetCustomAttributes(typeof(T), false);
        return attributes.Length > 0 ? (T)attributes[0] : null;
    }

    public static bool TrySingle<T>(this IEnumerable<T> list, Func<T, bool> predicate, [NotNullWhen(true)] out T value)
    {
#pragma warning disable CS8601
        value = list.SingleOrDefault(predicate);
#pragma warning restore CS8601
        return value is not null;
    }

    public static IEnumerable<(T, T)> GetPermutations<T>(this IEnumerable<T> items)
    {
        return items.GetPermutations(tup => (tup.Item1, tup.Item2));
    }

    public static IEnumerable<TOut> GetPermutations<T, TOut>(this IEnumerable<T> items, Func<(T, T), TOut> selector)
    {
        items = items.ToList();
        return items.SelectMany(m1 => items.Where(m2 => !m1?.Equals(m2) ?? false).Select(m2 => (m1, m2)))
            .Select(selector);
    }
}

/// <summary>Generic error type for any NecoBowl-caused exception.</summary>
public class NecoBowlException : ApplicationException
{
    public NecoBowlException()
    {
    }

    public NecoBowlException(string message) : base(message)
    {
    }

    public NecoBowlException(string message, Exception inner) : base(message, inner)
    {
    }
}
