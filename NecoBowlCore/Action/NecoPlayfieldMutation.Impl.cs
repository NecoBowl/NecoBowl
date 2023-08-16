using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlCore.Action;

public partial class NecoPlayfieldMutation
{
    public class UnitPushes : BaseMutation
    {
        public AbsoluteDirection Direction;
        public NecoUnitId Pusher;
        public NecoUnitId Receiver;

        public UnitPushes(NecoUnitId pusher, NecoUnitId receiver, AbsoluteDirection direction)
        {
            Pusher = pusher;
            Receiver = receiver;
            Direction = direction;
        }

        public override string Description
            => $"{Pusher} pushes {Receiver} to the {Direction}";
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

        public override string Description => $"{Subject} bumps to the {Direction}";
    }

    public class UnitAttacks : BaseMutation
    {
        public readonly NecoUnitId Attacker, Receiver;

        public readonly int Damage;

        public UnitAttacks(ReadOnlyNecoField field, NecoUnitId attacker, NecoUnitId receiver)
        {
            Attacker = attacker;
            Receiver = receiver;

            var unit = field.GetUnit(Attacker);
            Damage = unit.Power;
        }

        public override string Description => $"{Attacker} attacks {Receiver} for {Damage} damage";

        internal override IEnumerable<BaseMutation> GetResultantMutations(ReadOnlyNecoField field)
        {
            yield return new UnitTakesDamage(Receiver, (uint)Damage);
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

        public override string Description => $"{Subject} takes {DamageAmount} damage";

        internal override void Pass1Mutate(NecoField field)
        {
            var unit = field.GetUnit(Subject);
            unit.DamageTaken += (int)DamageAmount;
        }

        internal override IEnumerable<BaseMutation> GetResultantMutations(ReadOnlyNecoField field)
        {
            var unit = field.GetUnit(Subject);
            if (unit.CurrentHealth <= 0) {
                yield return new UnitDies(Subject);
            }
        }
    }

    public class UnitDies : BaseMutation
    {
        public readonly NecoUnitId Subject;

        public UnitDies(NecoUnitId subject)
        {
            Subject = subject;
        }

        public override string Description => $"{Subject} is destroyed";

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

        public override string Description => $"{Subject} gets {Mod}";

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
        public readonly NecoUnitMovement Source;
        public readonly NecoUnitId Subject;

        private NecoUnit? TempUnitItem;

        public UnitPicksUpItem(NecoUnitId subject, NecoUnitId item, NecoUnitMovement source)
        {
            Subject = subject;
            Item = item;
            Source = source;
        }

        public override string Description => $"{Subject} picks up {Item}";

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

        internal override IEnumerable<NecoPlayfieldMutation> GetResultantMutations(ReadOnlyNecoField field)
        {
            if (Source.IsChange) {
                yield return new MovementMutation(Source);
            }
        }
    }
}
