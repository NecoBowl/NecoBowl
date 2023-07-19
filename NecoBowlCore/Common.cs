using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("NecoBowlTest")]


namespace neco_soft.NecoBowlCore;

public enum AbsoluteDirection : uint
{
    South = 0,
    SouthWest = 1,
    West = 2,
    NorthWest = 3,
    North = 4,
    NorthEast = 5,
    East = 6,
    SouthEast = 7
}

public enum RelativeDirection : uint
{
    Up, UpRight, Right, DownRight, Down, DownLeft, Left, UpLeft
}

// ReSharper disable once InconsistentNaming
public readonly record struct Vector2i(int X, int Y)
{
    public int LengthSquared => this.X * this.X + this.Y * this.Y;
    public int Area => this.X * this.Y;

    public static readonly Vector2i Zero = new(0, 0);
    public static readonly Vector2i Up = new(0, 1);
    public static readonly Vector2i Right = new(1, 0);
    public static readonly Vector2i Down = new(0, -1);
    public static readonly Vector2i Left = new(-1, 0);

    public static Vector2i operator +(Vector2i left, Vector2i right)
        => new(left.X + right.X, left.Y + right.Y);
    public static Vector2i operator +(Vector2i left, int right)
        => new(left.X + right, left.Y + right);

    public static Vector2i operator -(Vector2i left, Vector2i right)
        => new(left.X - right.X, left.Y - right.Y);
    public static Vector2i operator -(Vector2i left, int right)
        => new(left.X - right, left.Y - right);

    public static Vector2i operator *(Vector2i left, Vector2i right)
        => new(left.X * right.X, left.Y * right.Y);
    public static Vector2i operator *(Vector2i left, int right)
        => new(left.X * right, left.Y * right);

    public static Vector2i operator /(Vector2i left, Vector2i right)
        => new(left.X / right.X, left.Y / right.Y);
    public static Vector2i operator /(Vector2i left, int right)
        => new(left.X / right, left.Y / right);

    public static implicit operator (int, int)(Vector2i input)
        => (input.X, input.Y);
    public static implicit operator Vector2i((int, int) input)
        => new(input.Item1, input.Item2);

    public override string ToString() => $"({X}, {Y})";
}

public static class AbsoluteDirectionExt
{
    public static AbsoluteDirection Opposite(this AbsoluteDirection direction)
        => direction.RotatedBy(4);

    public static AbsoluteDirection RotatedBy(this AbsoluteDirection direction, RelativeDirection rel)
        => RotatedBy(direction, (int)rel);
    public static AbsoluteDirection RotatedBy(this AbsoluteDirection direction, int rotation)
        => (AbsoluteDirection)(((uint)direction + rotation) % 8);

    // ReSharper disable once InconsistentNaming
    public static Vector2i ToVector2i(this AbsoluteDirection direction)
        => direction switch {
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

public static class Ext
{
    /// <summary>
    /// Gets an attribute on an enum field value
    /// </summary>
    /// <typeparam name="T">The type of the attribute you want to retrieve</typeparam>
    /// <param name="enumVal">The enum value</param>
    /// <returns>The attribute of type T that exists on the enum value</returns>
    /// <example><![CDATA[string desc = myEnumVariable.GetAttributeOfType<DescriptionAttribute>().Description;]]></example>
    public static T? GetAttributeOfType<T>(this Enum enumVal) where T:System.Attribute
    {
        var type = enumVal.GetType();
        var memInfo = type.GetMember(enumVal.ToString());
        var attributes = memInfo[0].GetCustomAttributes(typeof(T), false);
        return (attributes.Length > 0) ? (T)attributes[0] : null;
    }
}

/// <summary>
/// Generic error type for any NecoBowl-caused exception.
/// </summary>
public class NecoBowlException : ApplicationException
{
    public NecoBowlException() { }
    public NecoBowlException(string message) : base(message) { }
    public NecoBowlException(string message, Exception inner) : base(message, inner) { }
}