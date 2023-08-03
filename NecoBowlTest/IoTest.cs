using neco_soft.NecoBowlCore.Input;
using neco_soft.NecoBowlCore.Tactics;

namespace neco_soft.NecoBowlTest;

[TestFixture]
public abstract class IoTest
{
    private NecoBowlContext Context = null!;

    [SetUp]
    public void Setup()
    {
        Context = new NecoBowlContext(new NecoPlayerPair());
    }

    [TestFixture]
    public class OutputWrapperTests : IoTest
    {
        [Test]
        public void TurnInformation_HasInputs()
        {
            Context.AssertSendInput(new NecoInput.PlaceCard(Context.Players.Offense, TestCard(), (0, 0)));
            Assert.That(Context.GetPlayPreview().Field[0, 0], Is.Not.Null);
            var turnInfo = Context.GetTurn();
            Assert.That(turnInfo.CardPlayAt((0, 0)), Is.Not.Null);
        }
    }
    
    #region Helpers
    
    private NecoUnitCard TestCard(int cost = 0)
        => new NecoUnitCard(NecoCardModelCustom.FromUnitModel(NecoUnitModelCustom.DoNothing(), cost));
    
    #endregion
}