using NecoBowl.Core.Machine;
using NecoBowl.Core.Tags;
using Unit = NecoBowl.Core.Reports.Unit;

namespace NecoBowl.Core.Sport.Play;

public class UnitTakesDamage : BaseMutation
{
    public readonly uint DamageAmount;

    public UnitTakesDamage(Unit subject, uint damageAmount)
        : base(subject.Id)
    {
        DamageAmount = subject.Tags.Contains(NecoUnitTag.Invincible) ? 0 : damageAmount;
    }

    public override string Description => $"{Subject} takes {DamageAmount} damage";

    internal override void Pass1Mutate(Playfield field)
    {
        var unit = field.GetUnit(Subject);
        unit.DamageTaken += (int)DamageAmount;
    }

    internal override IEnumerable<BaseMutation> GetResultantMutations(ReadOnlyPlayfield field)
    {
        var unit = field.GetUnit(Subject);
        if (unit.CurrentHealth <= 0) {
            yield return new UnitDies(unit.ToReport());
        }
    }
}
