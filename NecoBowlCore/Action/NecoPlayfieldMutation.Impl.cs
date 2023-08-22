using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlCore.Action;

public abstract partial class NecoPlayfieldMutation
{
    public class UnitPushes : BaseMutation
    {
        public readonly AbsoluteDirection Direction;
        public readonly NecoUnitId Pusher;
        public readonly NecoUnitId Receiver;

        public UnitPushes(NecoUnitId pusher, NecoUnitId receiver, AbsoluteDirection direction)
            : base(pusher)
        {
            Pusher = pusher;
            Receiver = receiver;
            Direction = direction;
        }

        public override string Description
            => $"{Pusher} pushes {Receiver} to the {Direction}";

        internal override void PreMovementMutate(NecoField field, NecoSubstepContext substepContext)
        {
            var pusher = field.GetUnit(Pusher);
            var receiver = field.GetUnit(Receiver, out var receiverPos);
            if (field.IsInBounds(receiverPos + Direction.ToVector2i())) {
                substepContext.AddEntry(receiver.Id, new(receiver, receiverPos + Direction.ToVector2i(), receiverPos));
            }
        }
    }

    public class UnitBumps : BaseMutation
    {
        public readonly AbsoluteDirection Direction;

        public UnitBumps(NecoUnitId subject, AbsoluteDirection direction)
            : base(subject)
        {
            Direction = direction;
        }

        public override string Description => $"{Subject} bumps to the {Direction}";
    }

    public class UnitAttacks : BaseMutation
    {
        public readonly NecoUnitId Attacker, Receiver;
        public readonly Vector2i? AttackerOriginalDestination = null;

        public readonly int Damage;

        public UnitAttacks(ReadOnlyNecoField field, NecoUnitId attacker, NecoUnitId receiver)
            : base(attacker)
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

        public UnitTakesDamage(NecoUnitId subject, uint damageAmount)
            : base(subject)
        {
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
        public UnitDies(NecoUnitId subject)
            : base(subject)
        { }

        public override string Description => $"{Subject} is destroyed";

        internal override bool Prepare(NecoSubstepContext context, ReadOnlyNecoField field)
        {
            if (context.HasEntryOfType(Subject, typeof(UnitDies), this)) {
                return true;
            }

            return false;
        }

        internal override void PreMovementMutate(NecoField field, NecoSubstepContext substepContext)
        { }

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

        public UnitGetsMod(NecoUnitId subject, NecoUnitMod mod)
            : base(subject)
        {
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

        private NecoUnit? TempUnitItem;

        public UnitPicksUpItem(NecoUnitId subject, NecoUnitId item, NecoUnitMovement source)
            : base(subject)
        {
            Item = item;
            Source = source;
        }

        public override string Description => $"{Subject} picks up {Item}";

        internal override bool Prepare(NecoSubstepContext context, ReadOnlyNecoField field)
        {
            if (field.GetUnit(Subject).Carrier is not null) {
                throw new NecoBowlException("a unit with an inventory cannot be picked up");
            }

            return false;
        }

        internal override void Pass1Mutate(NecoField field)
        {
            var itemUnit = field.GetAndRemoveUnit(Item);
            TempUnitItem = itemUnit;
        }

        internal override void Pass3Mutate(NecoField field)
        {
            var subject = field.GetUnit(Subject);
            TempUnitItem!.Carrier = subject;
            subject.Inventory.Add(TempUnitItem!);
        }

        internal override IEnumerable<NecoPlayfieldMutation> GetResultantMutations(ReadOnlyNecoField field)
        {
            if (Source.IsChange) {
                yield return new MovementMutation(Source);
            }
        }
    }

    public class UnitHandsOffItem : BaseMutation
    {
        private readonly NecoUnitId Item;
        private readonly NecoUnitId Receiver;
        private NecoUnit? TempUnitItem;

        public UnitHandsOffItem(NecoUnitId subject, NecoUnitId receiver, NecoUnitId item) : base(subject)
        {
            Receiver = receiver;
            Item = item;
        }

        public override string Description => $"{Subject} hands off {Item} to {Receiver}";

        internal override void Pass1Mutate(NecoField field)
        {
            var passer = field.GetUnit(Subject);
            var itemUnit = passer.Inventory.Single(u => u.Id == Item);
            passer.Inventory.Remove(itemUnit);
            TempUnitItem = itemUnit;
            TempUnitItem.Carrier = null;
        }

        internal override void Pass3Mutate(NecoField field)
        {
            var receiver = field.GetUnit(Receiver);
            
        }
    }
}
