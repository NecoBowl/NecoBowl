using NecoBowl.Core.Input;
using NecoBowl.Core.Model;
using NecoBowl.Core.Sport.Tactics;

namespace neco_soft.NecoBowlTest;

[TestFixture]
public abstract class IoTest
{
#if false
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

    private UnitCard TestCard(int cost = 0)
    {
        return new(CardModelCustom.FromUnitModel(UnitModelCustomHealthEqualsPower.DoNothing(), cost));
    }
#endif
}
