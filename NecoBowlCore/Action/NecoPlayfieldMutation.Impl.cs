using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlCore.Action;

public partial class NecoPlayfieldMutation
{
    public class UnitPushes : BaseMutation
    {
        public NecoUnitId Pusher;

        public UnitPushes(NecoUnitId subject, Vector2i sourceSpace, Vector2i destSpace, NecoUnitId pusher)
        {
            Pusher = pusher;
        }
    }

    public class UnitBumps : BaseMutation
    {
        public readonly AbsoluteDirection Direction;
        public readonly NecoUnitId Subject;

        public UnitBumps(NecoUnitId subject, AbsoluteDirection direction)
        {
            Subject = subject;
            Direction = direction;
        }

        public override string ToString()
        {
            return $"{nameof(UnitBumps)} [{Subject} bumps into the {Direction} }}";
        }
    }

    public class UnitAttacks : BaseMutation
    {
        public readonly NecoUnitId Attacker, Receiver;

        public UnitAttacks(NecoUnitId attacker, NecoUnitId receiver)
        {
            Attacker = attacker;
            Receiver = receiver;
        }

        internal override void Pass3Mutate(NecoField field)
        { }

        internal override IEnumerable<BaseMutation> AddMutations(ReadOnlyNecoField field)
        {
            var unit = field.GetUnit(Attacker);
            yield return new UnitTakesDamage(Receiver, (uint)unit.Power);
        }

        public override string ToString()
        {
            return $"{nameof(UnitAttacks)} [{Attacker} attacks {Receiver}]";
        }
    }

    public class UnitTakesDamage : BaseMutation
    {
        public readonly uint DamageAmount;
        public readonly NecoUnitId Subject;

        public UnitTakesDamage(NecoUnitId subject, uint damageAmount)
        {
            Subject = subject;
            DamageAmount = damageAmount;
        }

        internal override void Pass1Mutate(NecoField field)
        {
            var unit = field.GetUnit(Subject);
            unit.DamageTaken += (int)DamageAmount;
        }

        internal override IEnumerable<BaseMutation> AddMutations(ReadOnlyNecoField field)
        {
            var unit = field.GetUnit(Subject);
            if (unit.CurrentHealth <= 0) yield return new UnitDies(Subject);
        }
    }

    public class UnitDies : BaseMutation
    {
        public readonly NecoUnitId Subject;

        public UnitDies(NecoUnitId subject)
        {
            Subject = subject;
        }

        internal override void Pass1Mutate(NecoField field)
        {
            field.GetAndRemoveUnit(Subject);
        }

        internal override void Pass2Mutate(NecoField field)
        { }
    }

    public class UnitGetsMod : BaseMutation
    {
        public readonly NecoUnitMod Mod;
        public readonly NecoUnitId Subject;

        public UnitGetsMod(NecoUnitId subject, NecoUnitMod mod)
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
        { }
    }

    public class UnitPicksUpItem : BaseMutation
    {
        public readonly NecoUnitId Item;
        public readonly NecoUnitId Subject;

        private NecoUnit? TempUnitItem;

        public UnitPicksUpItem(NecoUnitId subject, NecoUnitId item)
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
