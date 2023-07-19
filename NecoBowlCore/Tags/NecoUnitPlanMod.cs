using System.ComponentModel;
using System.Runtime.CompilerServices;

using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlCore.Tags;

public abstract class NecoUnitMod
{
    public NecoUnitMod() { }
    public abstract NecoUnitMod Apply<T>(T original) where T : NecoUnitMod;
    
    public sealed class Rotate : NecoUnitMod
    {
        public readonly int Rotation;

        public Rotate() : base() { }
        
        public Rotate(int rotation)
        {
            if (rotation is < 0 or > 7)
                throw new ArgumentException("rotation must be between 0 and 7");
            Rotation = rotation;
        }

        public override NecoUnitMod Apply<T>(T original)
        {
            if (original is not Rotate rotate) throw new InvalidModException();
            return new Rotate(Rotation + rotate.Rotation);
        }
    }

    public sealed class Flip : NecoUnitMod
    {
        public readonly bool EnableX, EnableY;

        public Flip() : base() { }
        public Flip(bool enableX, bool enableY)
        {
            EnableX = enableX;
            EnableY = enableY;
        }
        
        public override NecoUnitMod Apply<T>(T original)
        {
            if (original is not Flip flip) throw new InvalidModException();
            return new Flip(EnableX ^ flip.EnableX, EnableY ^ flip.EnableY);
        }
    }
}

public abstract class NecoCardOptionPermission
{
    public sealed class Rotate : NecoCardOptionPermission
    {
        public readonly int[] RotationsAllowed;
        public Rotate(int[] rotationsAllowed)
        { RotationsAllowed = rotationsAllowed; }
    }

    public sealed class FlipX : NecoCardOptionPermission
    { }
    public sealed class FlipY : NecoCardOptionPermission
    { }
}

public abstract class NecoCardOptionValue
{
    public sealed class Rotate : NecoCardOptionValue<NecoCardOptionPermission.Rotate>
    {
    }
}

public abstract class NecoCardOptionValue<T> : NecoCardOptionValue
{ }

public class InvalidModException : Exception
{
    public InvalidModException() { }
    public InvalidModException(string message) : base(message) { }
    public InvalidModException(string message, Exception inner) : base(message, inner) { }
}
