using NecoBowl.Core.Tags;

namespace NecoBowl.Core.Sport.Play;

public abstract partial class Mutation
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

        internal override void EarlyMutate(Playfield field, NecoSubstepContext substepContext)
        {
            var pusher = field.GetUnit(Pusher);
            var receiver = field.GetUnit(Receiver, out var receiverPos);
            if (field.IsInBounds(receiverPos + Direction.ToVector2i())) {
                substepContext.AddEntry(
                    receiver.Id, new() {
                        NewPos = receiverPos + Direction.ToVector2i(),
                        OldPos = receiverPos,
                        Unit = receiver
                    });
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
        public enum Kind
        {
            SpaceSwap, SpaceConflict
        }

        public readonly NecoUnitId Attacker, Receiver;
        public readonly Kind AttackKind;
        public readonly Vector2i? ConflictPosition;

        public readonly int Damage;

        public UnitAttacks(
            NecoUnitId attacker,
            NecoUnitId receiver,
            int attackDamage,
            Kind attackKind,
            Vector2i? conflictPosition)
            : base(attacker)
        {
            Attacker = attacker;
            Receiver = receiver;
            AttackKind = attackKind;
            ConflictPosition = conflictPosition;
            Damage = attackDamage;

            if (AttackKind == Kind.SpaceConflict && conflictPosition is null) {
                throw new("conflictPosition is required");
            }
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

        internal override void Pass1Mutate(Playfield field)
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
        {
        }

        public override string Description => $"{Subject} is destroyed";

        internal override NecoUnitId[] ExtractedUnits => new[] { Subject };

        internal override bool Prepare(NecoSubstepContext context, ReadOnlyNecoField field)
        {
            if (context.HasEntryOfType(Subject, typeof(UnitDies), this)) {
                return true;
            }

            return false;
        }

        internal override void Pass1Mutate(Playfield field)
        {
            var unit = field.GetAndRemoveUnit(Subject);
            field.GraveyardZone.Add(unit);
        }

        internal override void Pass2Mutate(Playfield field)
        {
        }
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

        internal override void Pass1Mutate(Playfield field)
        {
            var unit = field.GetUnit(Subject);
            unit.AddMod(Mod);
        }

        internal override void Pass2Mutate(Playfield field)
        {
        }
    }

    public class UnitPicksUpItem : BaseMutation
    {
        public readonly NecoUnitId Item;
        public readonly NecoUnitMovement Source;

        public UnitPicksUpItem(NecoUnitId subject, NecoUnitId item, NecoUnitMovement source)
            : base(subject)
        {
            Item = item;
            Source = source;
        }

        public override string Description => $"{Subject} picks up {Item}";

        internal override NecoUnitId[] ExtractedUnits => new[] { Item };

        internal override bool Prepare(NecoSubstepContext context, ReadOnlyNecoField field)
        {
            if (field.GetUnit(Subject).Carrier is not null) {
                throw new NecoBowlException("a unit with an inventory cannot be picked up");
            }

            return false;
        }

        internal override void Pass1Mutate(Playfield field)
        {
            var itemUnit = field.TempUnitZone.Single(u => u.Id == Item);
        }

        internal override void Pass3Mutate(Playfield field)
        {
            var itemUnit = field.TempUnitZone.Single(u => u.Id == Item);
            var subject = field.GetUnit(Subject);
            itemUnit!.Carrier = subject;
            subject.Inventory.Add(itemUnit!);
        }
    }

    public class UnitHandsOffItem : BaseMutation
    {
        public readonly NecoUnitId Item;
        public readonly NecoUnitId Receiver;
        private Unit? TempUnitItem;

        public UnitHandsOffItem(NecoUnitId subject, NecoUnitId receiver, NecoUnitId item) : base(subject)
        {
            Receiver = receiver;
            Item = item;
        }

        public override string Description => $"{Subject} hands off {Item} to {Receiver}";

        internal override void Pass1Mutate(Playfield field)
        {
            var passer = field.GetUnit(Subject);
            var itemUnit = passer.Inventory.Single(u => u.Id == Item);
            passer.Inventory.Remove(itemUnit);
            TempUnitItem = itemUnit;
            TempUnitItem.Carrier = null;
        }

        internal override void Pass3Mutate(Playfield field)
        {
            var receiver = field.GetUnit(Receiver);
            receiver.Inventory.Add(TempUnitItem!);
            TempUnitItem!.Carrier = receiver;
        }
    }

    public class UnitThrowsItem : BaseMutation
    {
        public readonly Vector2i Destination;
        public readonly NecoUnitId Item;

        public UnitThrowsItem(NecoUnitId subject, NecoUnitId item, Vector2i destination) : base(subject)
        {
            Item = item;
            Destination = destination;
        }

        public override string Description => $"{Subject} throws {Item} to {Destination}";

        internal override void Pass3Mutate(Playfield field)
        {
            // TODO Sanity check and make sure the item is in the Subject's inventory
            var itemUnit = field.GetUnit(Item);
            var subject = field.GetUnit(Subject);
            itemUnit.Carrier = null;
            subject.Inventory.Remove(itemUnit);

            var unitAtPosition = field.GetAllUnits(true)
                .Single(t => t.Item1 == Destination && t.Item2.HandoffItem() is null).Item2;
            unitAtPosition.Inventory.Add(itemUnit);
            itemUnit.Carrier = unitAtPosition;

            // carrier is adding itself
        }
    }
}
