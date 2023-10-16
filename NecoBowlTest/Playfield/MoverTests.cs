using NecoBowl.Core.Machine;
using NecoBowl.Core.Sport.Play;

namespace neco_soft.NecoBowlTest.Playfield;

[TestFixture]
internal class MoverTests
{
    [SetUp]
    public void SetUp()
    {
        Playfield = new(new((5, 5), (4, 4), 2));
    }

    public NecoBowl.Core.Machine.Playfield Playfield = null!;

    [Test]
    public void Mutation_DamageCanKill()
    {
        var unit1 = TestHelpers.UnitMover();
        var unit2 = TestHelpers.UnitMover();

        var damage = new UnitTakesDamage(unit1, 1);
        var unitMover = new PlayStepper(Playfield);
    }
}
