using NecoBowl.Core.Sport.Play;

namespace NecoBowl.Core.Machine.Mutations;

public class UnitAttacks : BaseMutation
{
    public readonly NecoUnitId Attacker, Receiver;
    public readonly Vector2i ConflictPosition;

    public UnitAttacks(
        Core.Reports.Unit attacker,
        Core.Reports.Unit receiver, 
        Vector2i conflictPosition)
        : base(attacker.Id)
    {
        ConflictPosition = conflictPosition;
        Attacker = attacker.Id;
        Receiver = receiver.Id;
    }

    public override string Description => $"{Attacker} attacks {Receiver}";

    internal override IEnumerable<BaseMutation> GetResultantMutations(ReadOnlyPlayfield field)
    {
        var unit = field.GetUnit(Attacker);
        var damage = Math.Max(unit.Power, 0);
        yield return new UnitTakesDamage(field.GetUnit(Receiver).ToReport(), (uint)damage);
    }
}
