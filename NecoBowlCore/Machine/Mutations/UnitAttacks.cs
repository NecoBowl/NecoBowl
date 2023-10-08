using NecoBowl.Core.Sport.Play;

namespace NecoBowl.Core.Machine.Mutations;

public class UnitAttacks : Mutation
{
    public readonly NecoUnitId Attacker, Receiver;

    public UnitAttacks(
        Core.Reports.Unit attacker,
        Core.Reports.Unit receiver)
        : base(attacker.Id)
    {
        Attacker = attacker.Id;
        Receiver = receiver.Id;
    }

    public override string Description => $"{Attacker} attacks {Receiver}";

    internal override IEnumerable<Mutation> GetResultantMutations(ReadOnlyPlayfield field)
    {
        var unit = field.GetUnit(Attacker);
        var damage = Math.Max(unit.Power, 0);
        yield return new UnitTakesDamage(field.GetUnit(Receiver).ToReport(), (uint)damage);
    }
}
