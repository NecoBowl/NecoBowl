using NecoBowl.Core.Sport.Play;

namespace NecoBowl.Core.Machine.Mutations;

public abstract class UnitAttacks : BaseMutation
{
    public readonly NecoUnitId Attacker, Receiver;

    public UnitAttacks(
        Reports.Unit attacker,
        Reports.Unit receiver)
        : base(attacker.Id)
    {
        Attacker = attacker.Id;
        Receiver = receiver.Id;
    }

    public override string Description => $"{Attacker} attacks {Receiver}";

    internal sealed override IEnumerable<BaseMutation> GetResultantMutations(ReadOnlyPlayfield field)
    {
        var unit = field.GetUnit(Attacker);
        var damage = Math.Max(unit.Power, 0);
        yield return new UnitTakesDamage(field.GetUnit(Receiver).ToReport(), (uint)damage);
    }
}

public class UnitAttacksOnSpace : UnitAttacks
{
    public readonly Vector2i ConflictPosition;

    public UnitAttacksOnSpace(Reports.Unit attacker, Reports.Unit receiver, Vector2i conflictPosition)
        : base(attacker, receiver)
    {
        ConflictPosition = conflictPosition;
    }
}

public class UnitAttacksBetweenSpaces : UnitAttacks
{
    public readonly (Vector2i, Vector2i) Spaces;

    public UnitAttacksBetweenSpaces(Reports.Unit attacker, Reports.Unit receiver, (Vector2i, Vector2i) spaces) : base(
        attacker, receiver)
    {
        var diff = spaces.Item1 - spaces.Item2;
        if (Math.Abs(diff.X) > 1 || Math.Abs(diff.Y) > 1) {
            throw new ArgumentException("spaces must be adjacent");
        }

        Spaces = spaces;
    }
}
