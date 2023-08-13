using neco_soft.NecoBowlCore;
using neco_soft.NecoBowlCore.Action;
using neco_soft.NecoBowlCore.Tactics;

namespace neco_soft.NecoBowlTest;

[TestFixture]
public class NewStepperTests
{
    [SetUp]
    public void SetUp()
    {
        Field = NewField();
        Play = new(Field);
    }

    internal static NecoField NewField()
    {
        return new(new((5, 5), (0, 0)));
    }

    internal static NecoField Field = null!;
    internal static NecoPlay Play = null!;

    private readonly NecoPlayer Player1 = new(), Player2 = new();

    [Test]
    public void TestCombat()
    {
        var unitA1 = new NecoUnit(NecoUnitModelCustom.Mover("MoverN", 5, 2));
        var unitA2 = new NecoUnit(NecoUnitModelCustom.Mover("MoverW", 5, 2, RelativeDirection.Left));
        Field[0, 0] = new(unitA1);
        Field[0, 1] = new(unitA2);

        var mutations = new Queue<NecoPlayfieldMutation>(Play.Step());

    }
}
