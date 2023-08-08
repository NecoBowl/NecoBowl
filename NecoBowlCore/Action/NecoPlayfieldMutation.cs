using System;

using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlCore.Action;

/// <summary>
/// 
/// </summary>
public abstract class NecoPlayfieldMutation
{
    internal virtual void Prepare(ReadOnlyNecoField field) { }
    internal virtual void Pass1Mutate(NecoField field) { }
    internal virtual void Pass2Mutate(NecoField field) { }
    internal virtual void Pass3Mutate(NecoField field) { }
    internal virtual NecoPlayfieldMutation[] ResultantMutations() => new NecoPlayfieldMutation[] { };

    internal static readonly Action<NecoPlayfieldMutation, NecoField>[] ExecutionOrder = new Action<NecoPlayfieldMutation, NecoField>[] {
        (m, f) => m.Prepare(f.AsReadOnly()),
        (m, f) => m.Pass1Mutate(f),
        (m, f) => m.Pass2Mutate(f),
        (m, f) => m.Pass3Mutate(f),
    };

    public class MoveUnit : NecoPlayfieldMutation
    {
        public readonly NecoUnitId Subject;
        public readonly Vector2i SourceSpace, DestSpace;

        private NecoUnit? TempUnit;
        
        public MoveUnit(NecoUnitId subject, Vector2i sourceSpace, Vector2i destSpace)
        {
            Subject = subject;
            SourceSpace = sourceSpace;
            DestSpace = destSpace;
        }
        
        internal override void Pass1Mutate(NecoField field)
        {
            var unit = field.GetAndRemoveUnit(Subject, out var position);
            if (position != SourceSpace) {
                throw new NecoPlayfieldMutationException($"mismatch between mutation and unit coordinates");
            }
            
            TempUnit = unit;
        }
        
        internal override void Pass2Mutate(NecoField field)
        {
            if (field.TryGetUnit(DestSpace, out var occupant)) {
                throw new NecoPlayfieldMutationException($"a unit is already on {DestSpace} ({occupant})");
            } 
            
            field[DestSpace] = field[DestSpace] with { Unit = TempUnit };
        }
    }

    public class PushUnit : MoveUnit
    {
        public NecoUnitId Pusher;
        
        public PushUnit(NecoUnitId subject, Vector2i sourceSpace, Vector2i destSpace, NecoUnitId pusher) 
            : base(subject, sourceSpace, destSpace)
        {
            Pusher = pusher;
        }
    }

    public class BumpUnits : NecoPlayfieldMutation
    {
        public NecoUnitId Unit1, Unit2;

        public BumpUnits(NecoUnitId unit1, NecoUnitId unit2)
        {
            Unit1 = unit1;
            Unit2 = unit2;
        }
    }

    public class FightUnits : NecoPlayfieldMutation
    {
        public readonly NecoUnitId Unit1, Unit2;

        public FightUnits(NecoUnitId unit1, NecoUnitId unit2)
        {
            Unit1 = unit1;
            Unit2 = unit2;
        }
    }
    
    public class DamageUnit : NecoPlayfieldMutation
    {
        public readonly NecoUnitId Subject;
        public readonly uint DamageAmount;
        
        public DamageUnit(NecoUnitId subject, uint damageAmount)
        {
            Subject = subject;
            DamageAmount = damageAmount;
        }

        internal override void Pass1Mutate(NecoField field)
        {
            var unit = field.GetUnit(Subject);
            unit.DamageTaken += (int)DamageAmount;
        }

        internal override void Pass2Mutate(NecoField field)
        {
        }
    }

    public class KillUnit : NecoPlayfieldMutation
    {
        public readonly NecoUnitId Subject;
        
        public KillUnit(NecoUnitId subject)
        {
            Subject = subject;
        }

        internal override void Pass1Mutate(NecoField field)
        {
            field.GetAndRemoveUnit(Subject);
        }

        internal override void Pass2Mutate(NecoField field)
        {
        }
    }
    
    public class ApplyModToUnit : NecoPlayfieldMutation
    {
        public readonly NecoUnitId Subject;
        public readonly NecoUnitMod Mod;
        
        public ApplyModToUnit(NecoUnitId subject, NecoUnitMod mod)
        {
            Subject = subject;
            Mod = mod;
        }

        internal override void Pass1Mutate(NecoField field)
        {
            var unit = field.GetUnit(Subject);
            unit.Mods.Add(Mod);
        }

        internal override void Pass2Mutate(NecoField field)
        {
        }
    }

    public class UnitPickedUpItem : NecoPlayfieldMutation
    {
        public readonly NecoUnitId Subject;
        public readonly NecoUnitId Item;

        private NecoUnit? TempUnitItem;

        public UnitPickedUpItem(NecoUnitId subject, NecoUnitId item)
        {
            Subject = subject;
            Item = item;
        }

        internal override void Pass1Mutate(NecoField field)
        {
            var itemUnit = field.GetAndRemoveUnit(Item);
            TempUnitItem = itemUnit;
        }

        internal override void Pass3Mutate(NecoField field)
        {
            var subject = field.GetUnit(Subject);
            subject.Inventory.Add(TempUnitItem!);
        }
    }
}

public class NecoPlayfieldMutationException : ApplicationException
{
    public NecoPlayfieldMutationException() { }
    public NecoPlayfieldMutationException(string message) : base(message) { }
    public NecoPlayfieldMutationException(string message, Exception inner) : base(message, inner) { }
}
