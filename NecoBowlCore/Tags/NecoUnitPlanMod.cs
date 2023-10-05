using NecoBowl.Core.Machine;
using NecoBowl.Core.Sport.Play;

namespace NecoBowl.Core.Tags;

/// <summary></summary>
/// <remarks>
/// Instances of these classes will be shared across a unit's mod list and a ModAdded EventHandler submission. For now I
/// just try not to allow mutability in the class. I should probably mandate a copy constructor and just copy instances of
/// these. I really wish these were structs...
/// </remarks>
public abstract class NecoUnitMod
{
    public virtual NecoUnitMod Update(Unit subject)
    {
        return this;
    }

    public abstract NecoUnitMod Apply<T>(T original) where T : NecoUnitMod;

    public sealed class Rotate : NecoUnitMod
    {
        public readonly int Rotation;

        public Rotate()
        {
        }

        public Rotate(int rotation)
        {
            Rotation = rotation % 8;
        }

        public override NecoUnitMod Update(Unit subject)
        {
            if (subject.GetMod<InvertRotation>().Enable) {
                return new Rotate(-Rotation);
            }

            return this;
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
        {
        }

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

    public sealed class InvertRotation : NecoUnitMod
    {
        public readonly bool Enable;

        public InvertRotation()
        {
        }

        public InvertRotation(bool enable)
        {
            Enable = enable;
        }

        public override NecoUnitMod Apply<T>(T original)
        {
            if (original is not InvertRotation rot) {
                throw new InvalidModException();
            }

            return new InvertRotation(Enable ^ rot.Enable);
        }
    }

    public sealed class OptionValues : NecoUnitMod
    {
        private readonly string Key;
        private readonly object Value;

        private Dictionary<string, object> OptionValueCollector = new();

        public OptionValues()
        {
        }

        public OptionValues(string key, object value)
        {
            Key = key;
            Value = value;
        }

        public override NecoUnitMod Apply<T>(T original)
        {
            if (original is not OptionValues optionValue) {
                throw new InvalidModException();
            }

            OptionValueCollector = new(optionValue.OptionValueCollector) { [Key] = Value };
            return this;
        }

        public object GetValue<T>(string key)
        {
            var value = GetValueOrNull<T>(key);
            if (value is not T) {
                throw new InvalidModException();
            }

            return value;
        }

        public object? GetValueOrNull<T>(string key)
        {
            var value = OptionValueCollector.GetValueOrDefault(key);
            if (value is null) {
                return null;
            }

            if (value is not T) {
                throw new InvalidModException();
            }

            return value;
        }
    }
}

/// <summary>
/// Definitions of a permission for a player-modifiable option on a card. Options are set by the player during Turns and
/// are applied to units at the start of the play. <p /> There are two main types of option permission:
/// <b>dictionary-type</b> (the default), and <b>apply-type</b>. Dictionary-type options simply add an entry in the
/// <see cref="NecoUnitMod.OptionValues" /> mod of a unit, which is a dictionary accessible by calling
/// <see cref="Unit.GetMod" /> with <see cref="NecoUnitMod.OptionValues" /> as the type. Apply-type options, on the
/// other hand, provide a function that is called at play start. The function takes a unit as input and performs whatever
/// manipulations it wants (typically, adding mods of a type other than <see cref="NecoUnitMod.OptionValues" />).
/// </summary>
/// <remarks>There are two ways an option</remarks>
public abstract class NecoCardOptionPermission
{
    public abstract string Identifier { get; }

    public abstract object Default { get; }

    public abstract object[] AllowedValues { get; }

    public abstract Type ArgumentType { get; }
    public abstract string AllowedValueVisual(object o);

    public abstract void ApplyToUnit(Unit unit, object val);

    public IEnumerable<NecoCardOptionItem> GetOptionItems()
    {
        return AllowedValues.Select(v => new NecoCardOptionItem(AllowedValueVisual(v), v));
    }

    public sealed class Rotate : DirectionOptionPermission
    {
        public static readonly string StaticIdentifier = nameof(Rotate);

        public Rotate(
            RelativeDirection[] rotationsAllowed,
            RelativeDirection @default = RelativeDirection.Up,
            string identifier = nameof(Rotate))
            : base(@default, identifier, rotationsAllowed)
        {
        }

        protected override void ApplyToUnit(Unit unit, RelativeDirection value)
        {
            unit.AddMod(new NecoUnitMod.Rotate((int)value));
        }
    }

    public sealed class InvertRotations : NecoCardOptionPermission<bool>
    {
        public InvertRotations(string identifier = nameof(InvertRotations))
            : base(false, identifier, new object[] { true, false })
        {
        }

        protected override void ApplyToUnit(Unit unit, bool value)
        {
            unit.AddMod(new NecoUnitMod.InvertRotation(value));
        }
    }

    public sealed class FlipX : BoolOptionPermission
    {
        public FlipX(string identifier = nameof(FlipX))
            : base(default, identifier)
        {
        }

        public override object[] AllowedValues { get; } = new[] { false, true }.Cast<object>()
            .ToArray();

        protected override void ApplyToUnit(Unit unit, bool value)
        {
            unit.AddMod(new NecoUnitMod.Flip(value, false));
        }
    }

    public sealed class FlipY : BoolOptionPermission
    {
        public FlipY(bool defaultValue, string identifier = nameof(FlipY))
            : base(defaultValue, identifier)
        {
        }

        protected override void ApplyToUnit(Unit unit, bool value)
        {
            unit.AddMod(new NecoUnitMod.Flip(false, value));
        }
    }

    public class BoolOptionPermission : NecoCardOptionPermission<bool>
    {
        protected BoolOptionPermission(bool defaultValue, string identifier)
            : base(defaultValue, identifier, new object[] { false, true })
        {
        }
    }

    public class DirectionOptionPermission : NecoCardOptionPermission<RelativeDirection>
    {
        public DirectionOptionPermission(
            RelativeDirection defaultvalue, string identifier, RelativeDirection[] allowedValues)
            : base(defaultvalue, identifier, allowedValues.Cast<object>().ToArray())
        {
        }

        protected override string AllowedValueVisual(RelativeDirection t)
        {
            return t.ToArrowGlyph();
        }
    }
}

public class NecoCardOptionPermission<T> : NecoCardOptionPermission
{
    public readonly T DefaultValue;

    protected NecoCardOptionPermission(T defaultValue, string identifier, object[] allowedValues)
    {
        Identifier = identifier;
        DefaultValue = defaultValue;
        AllowedValues = allowedValues;
    }

    public override object[] AllowedValues { get; }
    public override string Identifier { get; }
    public override object Default => DefaultValue!;

    public override Type ArgumentType => typeof(T);

    protected virtual string AllowedValueVisual(T t)
    {
        return t.ToString();
    }

    public virtual bool ValidateValueChange(T newValue)
    {
        return true;
    }

    protected virtual void ApplyToUnit(Unit unit, T value)
    {
        unit.AddMod(new NecoUnitMod.OptionValues(Identifier, value!));
    }

    public sealed override void ApplyToUnit(Unit unit, object val)
    {
        ApplyToUnit(unit, (T)val);
    }

    public override string AllowedValueVisual(object o)
    {
        return AllowedValueVisual((T)o);
    }
}

public record NecoCardOptionItem(string OptionDisplay, object OptionValue);

public class InvalidModException : Exception
{
    public InvalidModException()
    {
    }

    public InvalidModException(string message) : base(message)
    {
    }

    public InvalidModException(string message, Exception inner) : base(message, inner)
    {
    }
}

public class CardOptionException : Exception
{
    public CardOptionException()
    {
    }

    public CardOptionException(string message) : base(message)
    {
    }

    public CardOptionException(string message, Exception inner) : base(message, inner)
    {
    }
}
