using neco_soft.NecoBowlCore.Tactics;

using NUnit.Framework.Constraints;

using Constraint = NUnit.Framework.Constraints.Constraint;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace NecoBowlTest;

public abstract class FieldActionTests
{
    private static NecoField NewField() => new NecoField(5, 5);

    private static NecoField Field = null!;
    private static NecoPlay Play = null!;
    
    private readonly NecoPlayer Player1 = new(), Player2 = new();

    [SetUp]
    public void Setup()
    {
        Field = NewField();
        Play = new NecoPlay(Field);
    }

    #region Tests
    
    [TestFixture]
    private class Combat : FieldActionTests
    {
        /// <summary>
        /// Two units swapping spaces should fight.
        /// </summary>
        [Test, Combinatorial]
        public void SpaceSwapCausesCombat(
            [Values(1, 2)] int power1,
            [Values(1)] int power2)
        {
            // `A` units test equal power. `B` units test combat with a winner.
            var unitA1 = new NecoUnit(NecoUnitModelCustom.Mover("MoverN", power1, AbsoluteDirection.North));
            var unitA2 = new NecoUnit(NecoUnitModelCustom.Mover("MoverS", power2, AbsoluteDirection.South));

            Play.Step();
            
            Assert.Multiple(() =>
            {
                if (power1 == power2) {
                    Assert.That(unitA1, Is.Dead());
                    Assert.That(unitA2, Is.Dead());
                } else if (power1 > power2) {
                    Assert.That(unitA1.Health, Is.EqualTo(power1 - power2));
                    Assert.That(unitA2, Is.Dead());
                }
            });
        }

        /// <summary>
        /// Two units moving to the same space should fight.
        /// </summary>
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
            
            Assert.Multiple(() =>
            {
                Assert.That(unitA1, Is.Dead());
                Assert.That(unitA2, Is.Dead());
                Assert.That(unitB2, Is.Dead());
                Assert.That(unitB1, Is.AtFieldPosition((1, 1)));
                Assert.That(unitB1.Health, Is.EqualTo(2));
            });
        }

        /// <summary>
        /// After a space conflict in which both units die, another space conflict can occur on the same space.
        /// </summary>
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

            Assert.That(Field.GetAllUnits(), Is.Empty);
        }

        /// <summary>
        /// After a space conflict in which one unit survives, the surviving unit can fight another unit.
        /// </summary>
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
            
            Assert.Multiple(() =>
            {
                Assert.That(smallUnit1, Is.Dead());
                Assert.That(smallUnit2, Is.Dead());
                Assert.That(bigUnit, Is.AtFieldPosition((0, 1)));
                Assert.That(bigUnit.Health, Is.EqualTo(3));
            });
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
            
            Assert.Multiple(() =>
            {
                Assert.That(unit1, Is.AtFieldPosition((0, 1)));
                Assert.That(unit2, Is.AtFieldPosition((0, 2)));
            });
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
            
            Assert.Multiple(() =>
            {
                Assert.That(unit1, Is.Dead());
                Assert.That(unit2, Is.Dead());
                Assert.That(unit3, Is.AtFieldPosition((0, 1)));
            });
        }

        [Test]
        public void RotationAffectsMovementDirection()
        {
            var unit1 = new NecoUnit(NecoUnitModelCustom.Mover("MoverN_Strong", 69, AbsoluteDirection.North), Player1.Id);
            unit1.Mods.Add(new NecoUnitMod.Rotate((int)RelativeDirection.Right));
            Field[0, 0] = new(unit1);

            Play.Step();
            
            Assert.That(unit1, Is.AtFieldPosition((1, 0)));
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

            Assert.That(unit1, Is.AtFieldPosition((1, 1)));
        }

        [Test]
        public void BottommostUnitWins()
        {
            var unit1 = new NecoUnit(NecoUnitModelCustom.Mover("MoverN", 69, AbsoluteDirection.North), Player1.Id);
            var unit2 = new NecoUnit(NecoUnitModelCustom.Mover("MoverE", 69, AbsoluteDirection.East), Player1.Id);

            Field[1, 0] = new(unit1);
            Field[0, 1] = new(unit2);

            Play.Step();
            Assert.That(unit1, Is.AtFieldPosition((1, 1)));
        }

        [Test]
        public void SpaceSwapCausesNoMovement()
        {
            var unit1 = new NecoUnit(NecoUnitModelCustom.Mover("MoverN", 69, AbsoluteDirection.North), Player1.Id);
            var unit2 = new NecoUnit(NecoUnitModelCustom.Mover("MoverS", 69, AbsoluteDirection.South), Player1.Id);

            Field[0, 0] = new(unit1);
            Field[0, 1] = new(unit2);

            Play.Step();
            
            Assert.Multiple(() =>
            {
                Assert.That(unit1, Is.AtFieldPosition((0, 0)));
                Assert.That(unit2, Is.AtFieldPosition((0, 1)));
            });
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
            
            Assert.Multiple(() =>
            {
                Assert.That(unitBig, Is.AtFieldPosition((1, 2)));
                Assert.That(unitConflicter, Is.AtFieldPosition((1, 1)));
                Assert.That(unitSmall, Is.AtFieldPosition((1, 0)));
            });
            
            Play.Step();
            
            Assert.Multiple(() =>
            {
                Assert.That(unitBig, Is.AtFieldPosition((2, 2)));
                Assert.That(unitConflicter, Is.AtFieldPosition((1, 2)));
                Assert.That(unitSmall, Is.AtFieldPosition((1, 1)));
            });
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
            
            Assert.Multiple(() =>
            {
                Assert.That(unit1, Is.AtFieldPosition((0, 1)));
                Assert.That(unit2, Is.AtFieldPosition((0, 2)));
            });
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
            
            Assert.Multiple(() =>
            {
                Assert.That(pusher, Is.AtFieldPosition((0, 0)));
                Assert.That(receiver, Is.AtFieldPosition((1, 1)));
                Assert.That(enemy, Is.AtFieldPosition((0, 2)));
            });
        }
    }

    [TestFixture]
    private class Mods : FieldActionTests
    {
        [Test, Combinatorial]
        public void ModsGetApplied(
            [Values(1, 2, 3)] int mod1,
            [Values(1, 2, 3)] int mod2)
        {
            var unit = new NecoUnit(NecoUnitModelCustom.Pusher("MoverN", 69, AbsoluteDirection.North), Player1.Id);
            unit.Mods.Add(new NecoUnitMod.Rotate(mod1));
            unit.Mods.Add(new NecoUnitMod.Rotate(mod2));
            
            Assert.That(unit.GetMod<NecoUnitMod.Rotate>().Rotation, Is.EqualTo(mod1 + mod2));
        }
    }
    
    #endregion
    
    #region Helpers

    // ReSharper disable once ClassNeverInstantiated.Local
    private class Is : NUnit.Framework.Is
    {
        public static UnitDeadConstraint Dead()
            => new();

        public static UnitAtFieldPositionConstraint AtFieldPosition(Vector2i expected)
            => new(expected);
    }

    public class UnitDeadConstraint : Constraint
    {
        public override ConstraintResult ApplyTo<TActual>(TActual actual)
        {
            if (actual is NecoUnit unit) {
                return new(this, actual, !Field.TryGetUnit(unit.Id, out _));
            }
            return new(this, actual, false);
        }
    }

    public class UnitAtFieldPositionConstraint : Constraint
    {
        private readonly Vector2i Expected;
        
        public UnitAtFieldPositionConstraint(Vector2i expected)
        {
            Expected = expected;
        }
        
        public override ConstraintResult ApplyTo<TActual>(TActual actual)
        {
            if (actual is not NecoUnit unit) {
                return new(this, actual, ConstraintStatus.Error);
            }

            if (Field.TryGetUnit(unit.Id, out _, out var pos)) {
                return new(this, actual, pos == Expected);
            } else {
                return new(this, actual, ConstraintStatus.Error);
            }
        }
    }

    #endregion
}

public static class NUnitExtensions
{
    public static FieldActionTests.UnitDeadConstraint Dead(this ConstraintExpression expr)
        => new();

    public static FieldActionTests.UnitAtFieldPositionConstraint AtFieldPosition(this ConstraintExpression expr, Vector2i expected)
        => new(expected);
}
