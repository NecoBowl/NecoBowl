using NecoBowl.Core.Machine;

namespace NecoBowl.Core.Tags;

/// <summary>
/// Definition of a permission for a player-modifiable option on a card. Options are set by the player during Turns and are
/// applied to units at the start of the play. <p /> There are two main types of option permission: <b>dictionary-type</b>
/// (the default), and <b>apply-type</b>. Dictionary-type options simply add an entry in the
/// <see cref="UnitMod.OptionValues" /> mod of a unit, which is a dictionary accessible by calling
/// <see cref="Unit.GetMod" /> with <see cref="UnitMod.OptionValues" /> as the type. Apply-type options, on the other hand,
/// provide a function that is called at play start. The function takes a unit as input and performs whatever manipulations
/// it wants (typically, adding mods of a type other than <see cref="UnitMod.OptionValues" />).
/// </summary>
public abstract class CardOptionPermission
{
    public abstract string Identifier { get; }

    public abstract object Default { get; }

    public abstract object[] AllowedValues { get; }

    public abstract Type ArgumentType { get; }
    public abstract string AllowedValueVisual(object o);

    internal abstract void ApplyToUnit(Unit unit, object val);

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

        internal override void ApplyToUnit(Unit unit, RelativeDirection value)
        {
            unit.AddMod(new UnitMod.Rotate((int)value));
        }
    }

    public sealed class InvertRotations : BoolOptionPermission
    {
        public InvertRotations(string identifier = nameof(InvertRotations))
            : base(false, identifier)
        {
        }

        internal override void ApplyToUnit(Unit unit, bool value)
        {
            unit.AddMod(new UnitMod.InvertRotation(value));
        }
    }

    public sealed class FlipX : BoolOptionPermission
    {
        public FlipX(string identifier = nameof(FlipX))
            : base(default, identifier)
        {
        }

        internal override void ApplyToUnit(Unit unit, bool value)
        {
            unit.AddMod(new UnitMod.Flip(value, false));
        }
    }

    public sealed class FlipY : BoolOptionPermission
    {
        public FlipY(bool defaultValue, string identifier = nameof(FlipY))
            : base(defaultValue, identifier)
        {
        }

        internal override void ApplyToUnit(Unit unit, bool value)
        {
            unit.AddMod(new UnitMod.Flip(false, value));
        }
    }

    public class BoolOptionPermission : CardOptionPermission<bool>
    {
        protected BoolOptionPermission(bool defaultValue, string identifier)
            : base(defaultValue, identifier, new object[] { false, true })
        {
        }

        public override object[] AllowedValues { get; } = new[] { false, true }.Cast<object>()
            .ToArray();
    }

    public class DirectionOptionPermission : CardOptionPermission<RelativeDirection>
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

public class CardOptionPermission<T> : CardOptionPermission
{
    public readonly T DefaultValue;

    protected CardOptionPermission(T defaultValue, string identifier, object[] allowedValues)
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

    internal virtual void ApplyToUnit(Unit unit, T value)
    {
        unit.AddMod(new UnitMod.OptionValues(Identifier, value!));
    }

    internal sealed override void ApplyToUnit(Unit unit, object val)
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
