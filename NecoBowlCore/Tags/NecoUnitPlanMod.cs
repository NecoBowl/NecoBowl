using neco_soft.NecoBowlCore.Action;

namespace neco_soft.NecoBowlCore.Tags;

/// <summary>
/// </summary>
/// <remarks>
///     Instances of these classes will be shared across a unit's mod list and a ModAdded EventHandler submission. For now
///     I
///     just try not to allow mutability in the class. I should probably mandate a copy constructor and just copy instances
///     of
///     these.
///     I really wish these were structs...
/// </remarks>
public abstract class NecoUnitMod
{
    public abstract NecoUnitMod Apply<T>(T original) where T : NecoUnitMod;

    public sealed class Rotate : NecoUnitMod
    {
        public readonly int Rotation;

        public Rotate()
        { }

        public Rotate(int rotation)
        {
            Rotation = rotation % 8;
        }

        public override NecoUnitMod Apply<T>(T original)
        {
            if (original is not Rotate rotate) {
                throw new InvalidModException();
            }

            return new Rotate(Rotation + rotate.Rotation);
        }
    }

    public sealed class Flip : NecoUnitMod
    {
        public readonly bool EnableX, EnableY;

        public Flip()
        { }

        public Flip(bool enableX, bool enableY)
        {
            EnableX = enableX;
            EnableY = enableY;
        }

        public override NecoUnitMod Apply<T>(T original)
        {
            if (original is not Flip flip) {
                throw new InvalidModException();
            }

            return new Flip(EnableX ^ flip.EnableX, EnableY ^ flip.EnableY);
        }
    }
}

public abstract class NecoCardOptionPermission
{
    public abstract string Identifier { get; }

    public abstract object Default { get; }

    public abstract object[] AllowedValues { get; }

    public abstract Type ArgumentType { get; }
    public abstract string AllowedValueVisual(object o);

    public abstract void ApplyToUnit(NecoUnit unit, object val);

    public sealed class Rotate : NecoCardOptionPermission<int>
    {
        public static readonly string StaticIdentifier = nameof(Rotate);
        public readonly int[] RotationsAllowed;

        public Rotate(int[] rotationsAllowed, int @default, string identifier = nameof(Rotate))
            : base(@default, identifier)
        {
            RotationsAllowed = rotationsAllowed;
        }

        public override object[] AllowedValues => RotationsAllowed.Cast<object>().ToArray();

        protected override void ApplyToUnit(NecoUnit unit, int value)
        {
            unit.Mods.Add(new NecoUnitMod.Rotate(value));
        }

        protected override string AllowedValueVisual(int t)
        {
            return ((RelativeDirection)((int)RelativeDirection.Up + t)).ToArrowGlyph().ToString();
        }

        public override bool ValidateValueChange(int newValue)
        {
            return RotationsAllowed.Contains(newValue);
        }
    }

    public sealed class FlipX : NecoCardOptionPermission<bool>
    {
        public FlipX(string identifier = nameof(FlipX))
            : base(default, identifier)
        {
            Identifier = identifier;
        }

        public override string Identifier { get; }

        public override object[] AllowedValues { get; } = new[] { false, true }.Cast<object>()
            .ToArray();

        protected override void ApplyToUnit(NecoUnit unit, bool value)
        {
            unit.Mods.Add(new NecoUnitMod.Flip(value, false));
        }
    }

    public sealed class FlipY : NecoCardOptionPermission<bool>
    {
        public FlipY(bool defaultValue, string identifier = nameof(FlipY))
            : base(defaultValue, identifier)
        {
            Identifier = identifier;
        }

        public override string Identifier { get; }

        public override object[] AllowedValues { get; } = new[] { false, true }.Cast<object>()
            .ToArray();

        protected override void ApplyToUnit(NecoUnit unit, bool value)
        {
            unit.Mods.Add(new NecoUnitMod.Flip(false, value));
        }
    }
}

public abstract class NecoCardOptionPermission<T> : NecoCardOptionPermission
{
    public readonly T DefaultValue;

    protected NecoCardOptionPermission(T defaultValue, string identifier)
    {
        Identifier = identifier;
        DefaultValue = defaultValue;
    }

    public override string Identifier { get; }
    public override object Default => DefaultValue!;

    public override Type ArgumentType => typeof(T);

    public override string AllowedValueVisual(object o)
    {
        return AllowedValueVisual((T)o);
    }

    protected virtual string AllowedValueVisual(T t)
    {
        return t.ToString();
    }

    public virtual bool ValidateValueChange(T newValue)
    {
        return true;
    }

    public sealed override void ApplyToUnit(NecoUnit unit, object val)
    {
        ApplyToUnit(unit, (T)val);
    }

    protected abstract void ApplyToUnit(NecoUnit unit, T value);
}

public class InvalidModException : Exception
{
    public InvalidModException()
    { }

    public InvalidModException(string message) : base(message)
    { }

    public InvalidModException(string message, Exception inner) : base(message, inner)
    { }
}

public class CardOptionException : Exception
{
    public CardOptionException()
    { }

    public CardOptionException(string message) : base(message)
    { }

    public CardOptionException(string message, Exception inner) : base(message, inner)
    { }
}
