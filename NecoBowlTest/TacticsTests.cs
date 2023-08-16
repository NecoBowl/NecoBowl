using neco_soft.NecoBowlCore.Input;
using neco_soft.NecoBowlCore.Tactics;

using NLog;

namespace neco_soft.NecoBowlTest.Tactics;

internal abstract class TacticsTests
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private NecoBowlContext Context = null!;
    private NecoPlayerPair Players = null!;

    [SetUp]
    public void Setup()
    {
        Players = new(new(), new());
        Context = new(Players);
    }

    [TestFixture]
    private class Plan : TacticsTests
    {
        [Test]
        public void CardPlaysAppearInPlay()
        {
            var card = TestHelpers.TestCard();

            var resp = Context.SendInput(new NecoInput.PlaceCard(Players.Offense, card, (0, 0)));
            Assert.That(resp.ResponseKind, Is.EqualTo(NecoInputResponse.Kind.Success));

            Context.FinishTurn();
            var play = Context.BeginPlay();

            Assert.That(play.Field[0, 0].Unit, Is.Not.Null);
            Assert.That(play.Field[0, 0].Unit!.UnitModel, Is.EqualTo(card.UnitModel));
        }
    }

    private class Turn : TacticsTests
    {
        [Test]
        public void CardsCostMoney()
        {
            var card = TestHelpers.TestCard(1);

            var startingMoney = Context.Push.RemainingMoney(NecoPlayerRole.Offense);
            Context.AssertSendInput(new NecoInput.PlaceCard(Players.Offense, card, (0, 0)));

            Assert.That(Context.Push.RemainingMoney(NecoPlayerRole.Offense), Is.EqualTo(startingMoney - 1));
        }

        [Test]
        public void CannotOverspendOnTurn()
        {
            var card1 = TestHelpers.TestCard(2);
            var card2 = TestHelpers.TestCard(2);

            while (Context.Push.CurrentBaseMoney < 3) {
                Context.AdvancePush();
            }

            NecoInputResponse resp;

            Context.AssertSendInput(new NecoInput.PlaceCard(Players.Offense, card1, (0, 0)));
            Context.AssertSendInput(new NecoInput.PlaceCard(Players.Offense, card2, (0, 1)),
                NecoInputResponse.Kind.Illegal);

            Context.FinishTurn();

            Assert.That(Context.BeginPlay().Field[0, 1].Unit, Is.Null);
        }

        [Test]
        public void PreviewReflectsCurrentInputs()
        {
            var card1 = TestHelpers.TestCard(1);
            var card2 = TestHelpers.TestCard(1);

            Context.AssertSendInput(new NecoInput.PlaceCard(Players.Offense, card1, (0, 0)));
            Assert.That(Context.GetPlayPreview().Field[0, 0].Unit, Is.Not.Null);

            Context.AssertSendInput(new NecoInput.PlaceCard(Players.Offense, card2, (0, 1)));
            var preview = Context.GetPlayPreview();
            Assert.Multiple(() => {
                                Assert.That(preview.Field[0, 0].Unit, Is.Not.Null);
                                Assert.That(preview.Field[0, 1].Unit, Is.Not.Null);
                            });
        }
    }

    private class Push : TacticsTests
    {
        [Test]
        [Order(1)]
        public void TurnMustBeFinishedToShowPlay()
        {
            Assert.That(() => Context.AdvancePush(), Throws.InvalidOperationException);
        }

        [Test]
        [Order(2)]
        public void PlayMustBeRunToAdvanceTurn()
        {
            Context.FinishTurn();
            Assert.That(() => Context.AdvancePush(), Throws.InvalidOperationException);
            Context.BeginPlay();
        }

        [Test]
        [Order(3)]
        public void TurnMustBeFinishedToBeginPlay()
        {
            Assert.That(() => Context.BeginPlay(), Throws.InvalidOperationException);
        }

//        [Test]
        [Order(4)]
        public void PlayGoesToNextTurn()
        {
            var turnIndex = Context.Push.CurrentTurnIndex;
            Context.FinishTurn();
            var play = Context.BeginPlay();
            play.StepToFinish();
            Context.AdvancePush();
            Assert.That(Context.Push.CurrentTurnIndex, Is.EqualTo(turnIndex + 1));
        }
    }

#region Helpers

#endregion
}
