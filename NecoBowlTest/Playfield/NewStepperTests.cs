using neco_soft.NecoBowlCore.Tactics;

using NUnit.Framework.Constraints;

// ReSharper disable AccessToStaticMemberViaDerivedType

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

    private void SetUp_VerticalCollision(out NecoUnit unitA1, out NecoUnit unitA2)
    {
        unitA1 = NecoUnitModelCustom.Mover("MoverN", 5, 2).ToUnit(Player1);
        unitA2 = NecoUnitModelCustom.Mover("MoverS", 5, 2, RelativeDirection.Down).ToUnit(Player2);
        Field[0, 0] = new(unitA1);
        Field[0, 1] = new(unitA2);
    }

    [Test]
    public void Play_UnitCanAttackUnitBumpingIntoWall()
    {
        var unitA1 = NecoUnitModelCustom.Mover("MoverN", 5, 2).ToUnit(Player1);
        var unitA2 = NecoUnitModelCustom.Mover("MoverW", 5, 2, RelativeDirection.Left).ToUnit(Player2);
        Field[0, 0] = new(unitA1);
        Field[0, 1] = new(unitA2);

        var mutations = new Queue<NecoPlayfieldMutation>(Play.Step());
        Assert.That(
            mutations,
            Has.MutationWhere<NecoPlayfieldMutation.UnitBumps>(mut => mut.Direction == AbsoluteDirection.West));
        Assert.That(
            mutations,
            Has.MutationWhere<NecoPlayfieldMutation.UnitAttacks>(mut => mut.Attacker == unitA1.Id));
    }

    [Test]
    public void Play_UnitCanAttackUnitOnSpaceSwap()
    {
        SetUp_VerticalCollision(out var unitA1, out var unitA2);

        var mutations = new Queue<NecoPlayfieldMutation>(Play.Step());
        Assert.That(
            mutations,
            Has.MutationWhere<NecoPlayfieldMutation.UnitAttacks>(mut => mut.Attacker == unitA2.Id));
        Assert.That(
            mutations,
            Has.MutationWhere<NecoPlayfieldMutation.UnitAttacks>(mut => mut.Attacker == unitA1.Id));
    }

    [Test]
    public void Mutation_AttacksCauseUnitsToTakeDamage()
    {
        SetUp_VerticalCollision(out var unit1, out var unit2);

        Play.Step();

        Assert.That(unit1.CurrentHealth, Is.EqualTo(unit1.MaxHealth - unit2.Power));
    }

    [Test]
    public void Tag_Pusher_PushesUnitWhenCollidingPreemptively()
    {
        var unitA1 = new NecoUnit(NecoUnitModelCustom.Mover("MoverS", 5, 2, RelativeDirection.Down), Player1.Id);
        var unitA2 = new NecoUnit(
            NecoUnitModelCustom.Mover(
                "MoverN",
                5,
                2,
                tags: new[] { NecoUnitTag.Pusher }),
            Player1.Id);
        Field[0, 1] = new(unitA1);
        Field[0, 0] = new(unitA2);

        Play.Step();

        Assert.That(
            Field,
            Has.FieldContents(
                new() {
                    { (0, 2), unitA1 },
                    { (0, 1), unitA2 }
                }));

        Play.Step();

        Assert.That(
            Field,
            Has.FieldContents(
                new() {
                    { (0, 3), unitA1 },
                    { (0, 2), unitA2 }
                }));
    }

    [Test]
    public void Tag_Defender_DoesNotAttackWhenMovingIntoSpaceSwap()
    {
        var unitA1 = new NecoUnit(NecoUnitModelCustom.Mover("MoverS", 5, 2, RelativeDirection.Down), Player1.Id);
        var enemy = new NecoUnit(
            NecoUnitModelCustom.Mover(
                "MoverN",
                5,
                2,
                tags: new[] { NecoUnitTag.Defender }),
            Player2.Id);
        Field[0, 1] = new(unitA1);
        Field[0, 0] = new(enemy);

        var mutations = Play.Step().ToList();
        Assert.That(mutations, Has.MutationWhere<NecoPlayfieldMutation.UnitAttacks>(m => m.Attacker == unitA1.Id));
        Assert.That(mutations, Has.No.MutationWhere<NecoPlayfieldMutation.UnitAttacks>(m => m.Attacker == enemy.Id));
    }

    [Test]
    public void Tag_Defender_DoesNotAttackWhenMovingIntoSpaceConflict()
    {
        var other = new NecoUnit(NecoUnitModelCustom.Mover(null, 5, 2, RelativeDirection.Left), Player1.Id);
        var defender = new NecoUnit(
            NecoUnitModelCustom.Mover(
                "MoverN",
                5,
                2,
                tags: new[] { NecoUnitTag.Defender }),
            Player2.Id);
        Field[0, 0] = new(defender);
        Field[1, 1] = new(other);

        var mutations = Play.Step().ToList();
        Assert.That(mutations, Has.MutationWhere<NecoPlayfieldMutation.UnitAttacks>(m => m.Attacker == other.Id));
        Assert.That(mutations, Has.No.MutationWhere<NecoPlayfieldMutation.UnitAttacks>(m => m.Attacker == defender.Id));
    }

    [Test]
    public void Tag_Carrier_PickupOccursWhenMoveOntoItem()
    {
        var pickupper = new NecoUnit(
            NecoUnitModelCustom.Mover(
                "CarrierN",
                5,
                2,
                RelativeDirection.Up,
                new[] { NecoUnitTag.Carrier }),
            Player1.Id);
        var item = new NecoUnit(NecoUnitModelCustom.Item(), NecoPlayer.NeutralPlayer.Id);
        Field[0, 0] = new(pickupper);
        Field[0, 1] = new(item);

        var mutations = Play.Step().ToList();

        Assert.Multiple(
            () => {
                Assert.That(
                    mutations,
                    Has.MutationWhere<NecoPlayfieldMutation.UnitPicksUpItem>(m => m.Subject == pickupper.Id));
                Assert.That(pickupper.Inventory, Contains.Item(item));
            });
    }

    [Test]
    public void Tag_Bossy_UnitTakesTopPriority()
    {
        var unitControl1 = TestHelpers.UnitMover(player: Player1);
        var unitControl2 = TestHelpers.UnitMover(RelativeDirection.UpRight, player: Player1);

        var unitTest1 = TestHelpers.UnitMover(player: Player1);
        var unitTest2 = TestHelpers.UnitMover(
            RelativeDirection.UpRight,
            new[] { NecoUnitTag.Bossy },
            Player1);

        Field[1, 0] = new(unitControl1);
        Field[0, 0] = new(unitControl2);

        Field[3, 0] = new(unitTest1);
        Field[2, 0] = new(unitTest2);

        Play.Step();

        Assert.That(
            Field,
            Has.FieldContents(
                new() {
                    { (0, 0), unitControl2 },
                    { (1, 1), unitControl1 },
                    { (3, 1), unitTest2 },
                    { (3, 0), unitTest1 }
                }));
    }
}

#region Helpers

public class FieldHasContentsConstraint : Constraint
{
    private readonly Dictionary<Vector2i, NecoUnit> ExpectedContents;

    public FieldHasContentsConstraint(Dictionary<Vector2i, NecoUnit> expectedContents)
    {
        ExpectedContents = expectedContents;
    }

    public override ConstraintResult ApplyTo<TActual>(TActual actual)
    {
        if (actual is NecoField field) {
            foreach (var (pos, actualUnit) in field.GetAllUnits()) {
                if (ExpectedContents.TryGetValue(pos, out var expectedUnit)) {
                    if (expectedUnit != actualUnit) {
                        return new EqualConstraintResult(new(expectedUnit), actualUnit, false);
                    }
                }
                else {
                    return new(this, actualUnit, ConstraintStatus.Failure);
                }
            }

            return new(this, actual, ConstraintStatus.Success);
        }

        return new(this, actual, ConstraintStatus.Error);
    }
}

public class MutationListHasConstraint<T> : Constraint
    where T : NecoPlayfieldMutation
{
    private readonly Func<T, bool> Predicate;

    public MutationListHasConstraint(Func<T, bool> predicate)
    {
        Predicate = predicate;
    }

    public override ConstraintResult ApplyTo<TActual>(TActual actual)
    {
        if (actual is IEnumerable<NecoPlayfieldMutation> mutationList) {
            return new(this, actual, mutationList.OfType<T>().Any(Predicate));
        }

        return new(this, actual, ConstraintStatus.Failure);
    }
}

public abstract class Has : NUnit.Framework.Has
{
    public static MutationListHasConstraint<T> MutationWhere<T>(Func<T, bool> predicate)
        where T : NecoPlayfieldMutation
    {
        return new(predicate);
    }

    public static FieldHasContentsConstraint FieldContents(Dictionary<Vector2i, NecoUnit> expected)
    {
        return new(expected);
    }
}

public static class NUnitExt
{
    public static MutationListHasConstraint<T> MutationWhere<T>(this ConstraintExpression expr, Func<T, bool> predicate)
        where T : NecoPlayfieldMutation
    {
        var constraint = new MutationListHasConstraint<T>(predicate);
        expr.Append(constraint);
        return constraint;
    }

    public static FieldHasContentsConstraint HasFieldContents(this ConstraintExpression expr,
                                                              Dictionary<Vector2i, NecoUnit> expected)
    {
        var constraint = new FieldHasContentsConstraint(expected);
        expr.Append(constraint);
        return constraint;
    }
}

#endregion
