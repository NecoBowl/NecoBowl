namespace neco_soft.NecoBowlCore.Action;

/// <summary>
/// 
/// </summary>
/// <remarks>
/// Distinction between Mutations and Events:
/// 
/// Mutations represent the most generic method of modifying the field object. For example, Moving and Dying are
/// mutations, but Pushing is not, as it is an incidental event that occurs as a result of the Move.
/// </remarks>
public abstract class NecoPlayfieldMutation
{
    internal virtual void Pass1Mutate(NecoField field) { }
    internal virtual void Pass2Mutate(NecoField field) { }
    
    public class UnitMoved : NecoPlayfieldMutation
    {
        public readonly NecoUnitId Subject;
        public readonly Vector2i SourceSpace, DestSpace;

        private NecoUnit? TempUnit;
        
        public UnitMoved(NecoUnitId subject, Vector2i sourceSpace, Vector2i destSpace)
        {
            Subject = subject;
            SourceSpace = sourceSpace;
            DestSpace = destSpace;
        }
        
        override internal void Pass1Mutate(NecoField field)
        {
            var unit = field.GetAndRemoveUnit(Subject, out var position);
            if (position != SourceSpace) {
                throw new NecoPlayfieldMutationException($"mismatch between mutation and unit coordinates");
            }
            
            TempUnit = unit;
        }
        
        override internal void Pass2Mutate(NecoField field)
        {
            if (field.TryGetUnit(DestSpace, out var occupant)) {
                throw new NecoPlayfieldMutationException($"a unit is already on {DestSpace} ({occupant})");
            } 
            
            field[DestSpace] = field[DestSpace] with { Unit = TempUnit };
        }
    }

    public class UnitTookDamage : NecoPlayfieldMutation
    {
        public readonly NecoUnitId Subject;
        public readonly uint DamageAmount;
        
        public UnitTookDamage(NecoUnitId subject, uint damageAmount)
        {
            Subject = subject;
            DamageAmount = damageAmount;
        }

        override internal void Pass1Mutate(NecoField field)
        {
            var unit = field.GetUnit(Subject);
            unit.DamageTaken += (int)DamageAmount;
        }
    }

    public class UnitDied : NecoPlayfieldMutation
    {
        public readonly NecoUnitId Subject;
        
        public UnitDied(NecoUnitId subject)
        {
            Subject = subject;
        }

        override internal void Pass1Mutate(NecoField field)
        {
            field.GetAndRemoveUnit(Subject);
        }
    }
}

public class NecoPlayfieldMutationException : ApplicationException
{
    public NecoPlayfieldMutationException() { }
    public NecoPlayfieldMutationException(string message) : base(message) { }
    public NecoPlayfieldMutationException(string message, Exception inner) : base(message, inner) { }
}
