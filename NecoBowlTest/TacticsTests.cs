using System.Net.Mime;
using System.Text.RegularExpressions;

using neco_soft.NecoBowlCore.Input;
using neco_soft.NecoBowlCore.Tactics;

using NUnit.Framework.Constraints;

namespace NecoBowlTest;

public abstract class TacticsTests
{
    private NecoBowlContext Context;
    private NecoPlayerPair Players;

    public TacticsTests()
    {
        Players = new NecoPlayerPair(new(), new());
        Context = new NecoBowlContext(Players);
    }
    
    [TestFixture]
    private class Plan : TacticsTests
    {
        [Test]
        public void CardPlaysAppearInPlay()
        {
            var card = new NecoUnitCard(NecoCardModelCustom.FromUnitModel(NecoUnitModelCustom.DoNothing()));
            Context.SendInput(new NecoInput.PlaceCard(Players.Offense, card, (0, 0)));

            var play = Context.GetPlay();
            
            Assert.IsNotNull(Context.GetField(true)[0, 0].Unit);
            play.LogFieldToAscii();
        }

        
    }

    private class Conflicts : TacticsTests
    {
        [Test]
        public void CannotOverspendOnTurn()
        {
        }
    }
    
    #region Helpers
    
    private NecoUnitCard TestCard(int cost = 0)
        => new NecoUnitCard(NecoCardModelCustom.FromUnitModel(NecoUnitModelCustom.DoNothing(), cost));

    #endregion
}