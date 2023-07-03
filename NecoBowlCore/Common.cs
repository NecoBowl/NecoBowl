using System.Drawing;
using System.Numerics;
using System.Text;

using NLog.LayoutRenderers.Wrappers;

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

    public override string ToString() => $"({X}, {Y})";
}

public static class AbsoluteDirectionExt
{
    public static AbsoluteDirection Opposite(this AbsoluteDirection direction)
        => direction.RotatedBy(4);

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

/// <summary>
/// Generic error type for any NecoBowl-caused exception.
/// </summary>
public class NecoBowlException : ApplicationException
{
    public NecoBowlException() { }
    public NecoBowlException(string message) : base(message) { }
    public NecoBowlException(string message, Exception inner) : base(message, inner) { }
}
