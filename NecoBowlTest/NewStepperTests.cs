using neco_soft.NecoBowlCore.Tactics;

namespace neco_soft.NecoBowlTest;

[TestFixture]
public class NewStepperTests
{
    internal static NecoField NewField() => new NecoField(new((5, 5), (0, 0)));

    internal static NecoField Field = null!;
    internal static NecoPlay Play = null!;
    
    private readonly NecoPlayer Player1 = new(), Player2 = new();

    [SetUp]
    public void SetUp()
    {
        Field = NewField();
        Play = new NecoPlay(Field);
    }

    [Test]
    public void TestCombat()
    {
        var unitA1 = new NecoUnit(NecoUnitModelCustom.Mover("MoverN", 5, 2, RelativeDirection.Up));
        var unitA2 = new NecoUnit(NecoUnitModelCustom.Mover("MoverW", 5, 2, RelativeDirection.Left));
        Field[0, 0] = new(unitA1);
        Field[0, 1] = new(unitA2);

        var mutations = new Queue<NecoPlayfieldMutation>(Play.Step());
    }
}