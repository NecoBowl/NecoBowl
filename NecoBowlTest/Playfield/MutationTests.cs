namespace neco_soft.NecoBowlTest;

[TestFixture]
public class MutationTests
{
    [SetUp]
    public void SetUp()
    {
        SubstepContext = new();
        Unit1 = TestUnit();
        Unit2 = TestUnit();
    }

    private NecoSubstepContext SubstepContext = null!;
    private NecoUnit Unit1 = null!;
    private NecoUnit Unit2 = null!;

    [Test]
    public void CombatCausesDamage()
    { }

    private static NecoUnit TestUnit()
    {
        return new(NecoUnitModelCustom.Mover());
    }

    private void RunMutation(NecoPlayfieldMutation.BaseMutation mutation)
    {
        foreach (var pass in NecoPlayfieldMutation.ExecutionOrder) { }
    }
}
