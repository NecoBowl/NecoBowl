using NecoBowl.Core.Machine;

namespace NecoBowl.Core.Sport.Play;

public class UnitAttacks : Mutation
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

    internal override IEnumerable<Mutation> GetResultantMutations(ReadOnlyPlayfield field)
    {
        yield return new UnitTakesDamage(Receiver, (uint)Damage);
    }
}
