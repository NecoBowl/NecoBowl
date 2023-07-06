using System.Diagnostics.CodeAnalysis;

using neco_soft.NecoBowlCore.Tactics;

using NLog.LayoutRenderers.Wrappers;

namespace NecoBowlTest;

public abstract class FieldActionTests
{
    private static NecoField NewField() => new NecoField(5, 5);
    
    protected NecoPlay Play = null!;
    protected NecoField Field => Play.Field;

    protected NecoPlayer Player1 = new(), Player2 = new();

    [SetUp]
    public void Setup()
    {
        Play = new NecoPlay(NewField(), false);
    }

    #region Tests
    [TestFixture]
    private class Combat : FieldActionTests
    {
        [Test]
        public void SpaceSwapCausesCombat()
        {
            var unitA1 = new NecoUnit(NecoUnitModelCustom.Mover("MoverN", 69, AbsoluteDirection.North));
            var unitA2 = new NecoUnit(NecoUnitModelCustom.Mover("MoverS", 69, AbsoluteDirection.South));
            var unitB1 = new NecoUnit(NecoUnitModelCustom.Mover("MoverN_Winner", 5, AbsoluteDirection.North));
            var unitB2 = new NecoUnit(NecoUnitModelCustom.Mover("MoverS_Loser", 3, AbsoluteDirection.South));
            Field[0, 0] = new(unitA1);
            Field[0, 1] = new(unitA2);
            Field[1, 0] = new(unitB1);
            Field[1, 1] = new(unitB2);

            Play.Step();

            AssertUnitIsDead(unitA1);
            AssertUnitIsDead(unitA2);
            AssertUnitIsDead(unitB2);
            Assert.AreEqual(2, unitB1.Health);
        }

        [Test]
        public void SpaceConflictCausesCombat()
        {
            var unitA1 = new NecoUnit(NecoUnitModelCustom.Mover("MoverN", 69, AbsoluteDirection.North));
            var unitA2 = new NecoUnit(NecoUnitModelCustom.Mover("MoverS", 69, AbsoluteDirection.South));
            var unitB1 = new NecoUnit(NecoUnitModelCustom.Mover("MoverN_Winner", 5, AbsoluteDirection.North));
            var unitB2 = new NecoUnit(NecoUnitModelCustom.Mover("MoverS_Loser", 3, AbsoluteDirection.South));
            Field[0, 0] = new(unitA1);
            Field[0, 2] = new(unitA2);
            Field[1, 0] = new(unitB1);
            Field[1, 2] = new(unitB2);

            Play.Step();

            AssertUnitIsDead(unitA1);
            AssertUnitIsDead(unitA2);
            AssertUnitIsDead(unitB2);
            Assert.AreEqual(2, unitB1.Health);
        }

        [Test]
        public void SpaceConflictCanOccurMultipleTimes()
        {
            var unit1 = new NecoUnit(NecoUnitModelCustom.Mover("MoverN", 69, AbsoluteDirection.North), Player1.Id);
            var unit2 = new NecoUnit(NecoUnitModelCustom.Mover("SouthMover", 69, AbsoluteDirection.South), Player2.Id);
            var unit3 = new NecoUnit(NecoUnitModelCustom.Mover("WestMover", 1, AbsoluteDirection.West), Player1.Id);
            var unit4 = new NecoUnit(NecoUnitModelCustom.Mover("EastMover", 1, AbsoluteDirection.East), Player2.Id);
            Field[1, 1] = new(unit1);
            Field[1, 3] = new(unit2);
            Field[2, 2] = new(unit3);
            Field[0, 2] = new(unit4);

            Play.Step();

            Assert.IsFalse(Field.GetAllUnits().Any(), "no units remain");
        }

        [Test]
        public void OneUnitCanFightMultipleOthers()
        {
            var bigUnit = new NecoUnit(NecoUnitModelCustom.DoNothing("DoNothing_Strong", 5), Player1.Id);
            var smallUnit1 = new NecoUnit(NecoUnitModelCustom.Mover("SouthMover_Weak", 1, AbsoluteDirection.South),
                Player2.Id);
            var smallUnit2 = new NecoUnit(NecoUnitModelCustom.Mover("WestMover_Weak", 1, AbsoluteDirection.West),
                Player2.Id);

            Field[0, 1] = new(bigUnit);
            Field[0, 2] = new(smallUnit1);
            Field[1, 1] = new(smallUnit2);

            Play.Step();

            Assert.IsTrue(Field[0, 1].Unit == bigUnit, "the unit at (0,1) is the big unit");
            Assert.IsTrue(bigUnit.Health == 3, "the big unit has 5 - 2 = 3 health");
        }
    }

    [TestFixture]
    private class Movement : FieldActionTests
    {
        [Test]
        public void UnitCanMoveFollowingBehindAnother()
        {
            var unit1 = new NecoUnit(NecoUnitModelCustom.Mover("MoverN", 69, AbsoluteDirection.North));
            var unit2 = new NecoUnit(NecoUnitModelCustom.Mover("MoverN", 69, AbsoluteDirection.North));
            Field[0, 0] = new(unit1);
            Field[0, 1] = new(unit2);

            Play.Step();

            Assert.IsTrue(Field[0, 0].Unit is null);
            Assert.IsTrue(Field[0, 1].Unit == unit1);
            Assert.IsTrue(Field[0, 2].Unit == unit2);
        }

        [Test]
        public void WeakerUnitCanTakeSpaceAfterStrongerUnitsFight()
        {
            var unit1 = new NecoUnit(NecoUnitModelCustom.Mover("MoverN_Strong", 69, AbsoluteDirection.North), Player1.Id);
            var unit2 = new NecoUnit(NecoUnitModelCustom.Mover("MoverS_Strong", 69, AbsoluteDirection.South), Player2.Id);
            var unit3 = new NecoUnit(NecoUnitModelCustom.Mover("MoverW_Weak", 1, AbsoluteDirection.West), Player1.Id);
            Field[0, 0] = new(unit1);
            Field[0, 2] = new(unit2);
            Field[1, 1] = new(unit3);

            Play.Step();

            Assert.IsTrue(Field[0, 1].Unit == unit3);
        }

        [Test]
        public void RotationAffectsMovementDirection()
        {
            var unit1 = new NecoUnit(NecoUnitModelCustom.Mover("MoverN_Strong", 69, AbsoluteDirection.North), Player1.Id);
            unit1.Mods.Add(new NecoUnitMod.Rotate((int)RelativeDirection.Right));
            Field[0, 0] = new(unit1);

            Play.Step();
            
            AssertUnitPosition(unit1, new(1, 0));
        }
    }

    [TestFixture]
    private class SpaceConflict : FieldActionTests
    {
        [Test]
        public void LeftmostUnitWins()
        {
            var unit1 = new NecoUnit(NecoUnitModelCustom.Mover("MoverNE", 69, AbsoluteDirection.NorthEast), Player1.Id);
            var unit2 = new NecoUnit(NecoUnitModelCustom.Mover("MoverN", 69, AbsoluteDirection.North), Player1.Id);

            Field[0, 0] = new(unit1);
            Field[1, 0] = new(unit2);

            Play.Step();

            Assert.IsTrue(Field[1, 1].Unit == unit1, "the leftmost unit took the space");
        }

        [Test]
        public void BottommostUnitWins()
        {
            var unit1 = new NecoUnit(NecoUnitModelCustom.Mover("MoverN", 69, AbsoluteDirection.North), Player1.Id);
            var unit2 = new NecoUnit(NecoUnitModelCustom.Mover("MoverE", 69, AbsoluteDirection.East), Player1.Id);

            Field[1, 0] = new(unit1);
            Field[0, 1] = new(unit2);

            Play.Step();
            Assert.IsTrue(Field[1, 1].Unit == unit1, "the bottommost unit took the space");
        }

        [Test]
        public void SpaceSwapCausesNoMovement()
        {
            var unit1 = new NecoUnit(NecoUnitModelCustom.Mover("MoverN", 69, AbsoluteDirection.North), Player1.Id);
            var unit2 = new NecoUnit(NecoUnitModelCustom.Mover("MoverS", 69, AbsoluteDirection.South), Player1.Id);

            Field[0, 0] = new(unit1);
            Field[0, 1] = new(unit2);

            Play.Step();
            Assert.IsTrue(Field[0, 0].Unit == unit1);
            Assert.IsTrue(Field[0, 1].Unit == unit2);
        }

        [Test]
        public void BackupSituationResolvesCorrectly()
        {
            // Takes the space from Conflicter
            var unitBig = new NecoUnit(NecoUnitModelCustom.Mover("BigFella", 69, AbsoluteDirection.East), Player1.Id);
            // Attempts to move into the space that Big takes
            var unitConflicter = new NecoUnit(NecoUnitModelCustom.Mover("Conflicter", 5, AbsoluteDirection.North),
                Player1.Id);
            // Tries to move into Conflicter's space
            var unitSmall = new NecoUnit(NecoUnitModelCustom.Mover("LittleMan", 1, AbsoluteDirection.North), Player1.Id);

            Field[0, 2] = new(unitBig); // moves to (1, 2)
            Field[1, 1] = new(unitConflicter); // moves to (1, 2)
            Field[1, 0] = new(unitSmall); // moves to (1, 1)

            Play.Step();

            Assert.IsTrue(Field[1, 2].Unit == unitBig);
            Assert.IsTrue(Field[1, 1].Unit == unitConflicter);
            Assert.IsTrue(Field[1, 0].Unit == unitSmall);

            Play.Step();

            Assert.IsTrue(Field[2, 2].Unit == unitBig);
            Assert.IsTrue(Field[1, 2].Unit == unitConflicter);
            Assert.IsTrue(Field[1, 1].Unit == unitSmall);
        }

    }

    [TestFixture]
    private class Pushing : FieldActionTests
    {
        [Test]
        public void FriendlyUnitsCanPush()
        {
            var unit1 = new NecoUnit(NecoUnitModelCustom.Pusher("MoverN", 69, AbsoluteDirection.North), Player1.Id);
            var unit2 = new NecoUnit(NecoUnitModelCustom.DoNothing("Nothing", 69), Player1.Id);

            Field[0, 0] = new(unit1);
            Field[0, 1] = new(unit2);

            Play.Step();

            Assert.IsTrue(Field[0, 1].Unit == unit1);
            Assert.IsTrue(Field[0, 2].Unit == unit2);
        }

        [Test]
        public void CannotPushFriendlyIntoEnemy()
        {
            var pusher = new NecoUnit(NecoUnitModelCustom.Pusher("MoverN", 69, AbsoluteDirection.North), Player1.Id);
            var receiver = new NecoUnit(NecoUnitModelCustom.Mover("MoverE", 69, AbsoluteDirection.East), Player1.Id);
            var enemy = new NecoUnit(NecoUnitModelCustom.DoNothing("EnemyWall", 70), Player2.Id);

            Field[0, 0] = new(pusher);
            Field[0, 1] = new(receiver);
            Field[0, 2] = new(enemy);
            
            Play.Step();
            
            AssertUnitPosition(pusher, new(0, 0));
            AssertUnitPosition(receiver, new(1, 1));
            AssertUnitPosition(enemy, new(0, 2));
        }
    }

    [TestFixture]
    private class Misc : FieldActionTests
    {
        [Test]
        public void ModsGetApplied()
        {
            var unit = new NecoUnit(NecoUnitModelCustom.Pusher("MoverN", 69, AbsoluteDirection.North), Player1.Id);
            unit.Mods.Add(new NecoUnitMod.Rotate(2));
            Assert.AreEqual(2, unit.GetMod<NecoUnitMod.Rotate>().Rotation);
        }
    }
    #endregion
    
    #region Helpers

    protected void AssertUnitIsDead(NecoUnit unit)
    {
        Assert.IsTrue(!Field.TryGetUnit(unit.Id, out _));
    }

    protected void AssertUnitPosition(NecoUnit unit, Vector2i position)
    {
        Assert.AreEqual(unit, Field[position].Unit);
    }

    #endregion
}