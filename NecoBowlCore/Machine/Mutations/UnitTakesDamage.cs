using NecoBowl.Core.Machine;

namespace NecoBowl.Core.Sport.Play;

public class UnitTakesDamage : Mutation
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

    internal override IEnumerable<Mutation> GetResultantMutations(ReadOnlyPlayfield field)
    {
        var unit = field.GetUnit(Subject);
        if (unit.CurrentHealth <= 0) {
            yield return new UnitDies(Subject);
        }
    }
}
