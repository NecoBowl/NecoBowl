using NecoBowl.Core.Input;
using NecoBowl.Core.Model;
using NecoBowl.Core.Sport.Tactics;

namespace neco_soft.NecoBowlTest;

[TestFixture]
public abstract class IoTest
{
    [SetUp]
    public void Setup()
    {
        Context = new(new());
    }

    private NecoBowlContext Context = null!;

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

    private NecoUnitCard TestCard(int cost = 0)
    {
        return new(NecoCardModelCustom.FromUnitModel(NecoUnitModelCustom_HealthEqualsPower.DoNothing(), cost));
    }
}
