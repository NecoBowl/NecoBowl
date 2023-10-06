using NecoBowl.Core.Machine;

namespace NecoBowl.Core.Tags;

public abstract class UnitMod
{
    internal virtual UnitMod Update(Unit subject)
    {
        return this;
    }

    public abstract UnitMod Apply<T>(T original) where T : UnitMod;

    public sealed class Rotate : UnitMod
    {
        public readonly int Rotation;

        public Rotate()
        {
        }

        public Rotate(int rotation)
        {
            Rotation = rotation % 8;
        }

        internal override UnitMod Update(Unit subject)
        {
            if (subject.GetMod<InvertRotation>().Enable) {
                return new Rotate(-Rotation);
            }

            return this;
        }

        public override UnitMod Apply<T>(T original)
        {
            if (original is not Rotate rotate) {
                throw new InvalidModException();
            }

            return new Rotate(Rotation + rotate.Rotation);
        }
    }

    public sealed class Flip : UnitMod
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

        public override UnitMod Apply<T>(T original)
        {
            if (original is not Flip flip) {
                throw new InvalidModException();
            }

            return new Flip(EnableX ^ flip.EnableX, EnableY ^ flip.EnableY);
        }
    }

    public sealed class InvertRotation : UnitMod
    {
        public readonly bool Enable;

        public InvertRotation()
        {
        }

        public InvertRotation(bool enable)
        {
            Enable = enable;
        }

        public override UnitMod Apply<T>(T original)
        {
            if (original is not InvertRotation rot) {
                throw new InvalidModException();
            }

            return new InvertRotation(Enable ^ rot.Enable);
        }
    }

    public sealed class OptionValues : UnitMod
    {
        private readonly string Key;
        private readonly object Value;

        private Dictionary<string, object> OptionValueCollector = new();

        public OptionValues() { }

        public OptionValues(string key, object value)
        {
            Key = key;
            Value = value;
        }

        public override UnitMod Apply<T>(T original)
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
