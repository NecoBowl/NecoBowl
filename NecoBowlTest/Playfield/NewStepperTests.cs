using System.Collections.ObjectModel;

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
        var unitNorth = NecoUnitModelCustom.Mover("MoverN", 5, 2).ToUnit(Player1);
        var unitWest = NecoUnitModelCustom.Mover("MoverW", 5, 2, RelativeDirection.Left).ToUnit(Player2);
        Field[0, 0] = new(unitNorth);
        Field[0, 1] = new(unitWest);

        var mutations = new Queue<NecoPlayfieldMutation>(Play.Step());
        Assert.That(
            mutations,
            Has.EquivalentMutationsTo(
                new NecoPlayfieldMutation.UnitBumps(unitWest.Id, AbsoluteDirection.West),
                new NecoPlayfieldMutation.UnitAttacks(
                    unitNorth.Id,
                    unitWest.Id,
                    unitNorth.Power,
                    NecoPlayfieldMutation.UnitAttacks.Kind.SpaceConflict,
                    (0, 1)),
                new NecoPlayfieldMutation.UnitTakesDamage(unitWest.Id, (uint)unitNorth.Power)));
    }

    [Test]
    public void Play_UnitCanAttackUnitOnSpaceSwap()
    {
        SetUp_VerticalCollision(out var unitA1, out var unitA2);

        var mutations = new Queue<NecoPlayfieldMutation>(Play.Step());
        Assert.That(
            mutations,
            Has.EquivalentMutationsTo(
                new NecoPlayfieldMutation.UnitAttacks(
                    unitA1.Id,
                    unitA2.Id,
                    unitA1.Power,
                    NecoPlayfieldMutation.UnitAttacks.Kind.SpaceSwap,
                    null),
                new NecoPlayfieldMutation.UnitAttacks(
                    unitA2.Id,
                    unitA1.Id,
                    unitA2.Power,
                    NecoPlayfieldMutation.UnitAttacks.Kind.SpaceSwap,
                    null),
                new NecoPlayfieldMutation.UnitTakesDamage(unitA1.Id, (uint)unitA2.Power),
                new NecoPlayfieldMutation.UnitTakesDamage(unitA2.Id, (uint)unitA1.Power)));
    }

    [Test]
    public void Mutation_AttacksCauseUnitsToTakeDamage()
    {
        SetUp_VerticalCollision(out var unit1, out var unit2);

        Play.Step();

        Assert.That(unit1.CurrentHealth, Is.EqualTo(unit1.MaxHealth - unit2.Power));
    }

    [Test]
    public void Movement_TrailingUnitsCanMove()
    {
        var unit1 = NecoUnitModelCustom.Mover().ToUnit(Player1);
        var unit2 = NecoUnitModelCustom.Mover().ToUnit(Player1);

        Field[0, 0] = new(unit1);
        Field[0, 1] = new(unit2);

        var muts = Play.Step();

        Assert.That(
            muts,
            Has.EquivalentMutationsTo(
                new NecoPlayfieldMutation.MovementMutation(new(unit1, (0, 1), (0, 0))),
                new NecoPlayfieldMutation.MovementMutation(new(unit2, (0, 2), (0, 1)))));
    }

    [Test]
    public void Movement_ItemPushedOntoConflictSpaceGetsPickedUp()
    {
        var carrier = NecoUnitModelCustom.Mover(
                "Carrier",
                direction: RelativeDirection.Up,
                tags: new[] { NecoUnitTag.Carrier })
            .ToUnit(Player1);
        var pusher = NecoUnitModelCustom.Mover(
                "Pusher",
                direction: RelativeDirection.Right,
                tags: new[] { NecoUnitTag.Pusher })
            .ToUnit(Player1);
        var item = new NecoUnit(NecoUnitModelCustom.Item(), Player1.Id);

        Field[0, 2] = new(pusher);
        Field[2, 1] = new(carrier);
        Field[1, 2] = new(item);

        var mutations = Play.Step().ToList();
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

        Assert.That(
            Play.Step(),
            Has.EquivalentMutationsTo(
                new NecoPlayfieldMutation.UnitAttacks(
                    unitA1.Id,
                    enemy.Id,
                    unitA1.Power,
                    NecoPlayfieldMutation.UnitAttacks.Kind.SpaceSwap,
                    null),
                new NecoPlayfieldMutation.UnitBumps(enemy.Id, AbsoluteDirection.North)));
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

        var movement = new NecoUnitMovement(pickupper, (0, 1), (0, 0));
        Assert.That(
            mutations,
            Has.EquivalentMutationsTo(
                new NecoPlayfieldMutation.MovementMutation(movement),
                new NecoPlayfieldMutation.UnitPicksUpItem(pickupper.Id, item.Id, movement)));
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

public record MutationChecker
{
    public readonly NecoPlayfieldMutation Mutation;

    public MutationChecker(NecoPlayfieldMutation mutation)
    {
        Mutation = mutation;
    }

    public bool EqualsMutation(NecoPlayfieldMutation mutation)
    {
        if (Mutation.GetType() != mutation.GetType()) {
            return false;
        }

        foreach (var field in Mutation.GetType().GetProperties()) {
            if (!field.GetValue(Mutation)!.Equals(field.GetValue(mutation))) {
                return false;
            }
        }

        return true;
    }
}

#region Helpers

/// <summary>
///     Checks that each space coordinate in the given dictionary has its associated contents in a <see cref="NecoField" />
///     .
/// </summary>
public class FieldHasContentsConstraint : Constraint
{
    private readonly Dictionary<Vector2i, NecoUnit> ExpectedContents;

    public FieldHasContentsConstraint(Dictionary<Vector2i, NecoUnit> expectedContents)
    {
        ExpectedContents = expectedContents;
    }

    public override ConstraintResult ApplyTo<TActual>(TActual actual)
    {
        if (actual is not NecoField field) {
            return new(this, actual, ConstraintStatus.Error);
        }

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
}

/// <summary>
///     Checks if a mutation list has a mutation that passes a given predicate.
/// </summary>
public class MutationListHasConstraint : Constraint
{
    // jank
    private readonly Func<object, bool>? Predicate;

    public MutationListHasConstraint()
    { }

    public MutationListHasConstraint(Func<object, bool> predicate)
    {
        Predicate = predicate;
    }

    public override ConstraintResult ApplyTo<TActual>(TActual actual)
    {
        if (actual is IEnumerable<NecoPlayfieldMutation> mutationList) {
            return new(this, actual, mutationList.Any(Predicate!));
        }

        return new(this, actual, ConstraintStatus.Failure);
    }
}

/// <inheritdoc cref="MutationListHasConstraint" />
/// <typeparam name="T">The type of the mutation.</typeparam>
public class MutationListHasConstraint<T> : MutationListHasConstraint
    where T : NecoPlayfieldMutation
{
    private readonly Func<T, bool> Predicate;

    public MutationListHasConstraint(Func<T, bool> predicate)
    {
        Predicate = predicate;
    }

    public override string Description => $"[{typeof(T)}]";

    public override ConstraintResult ApplyTo<TActual>(TActual actual)
    {
        if (actual is IEnumerable<NecoPlayfieldMutation> mutationList) {
            return new(this, actual, mutationList.OfType<T>().Any(Predicate));
        }

        return new(this, actual, ConstraintStatus.Failure);
    }
}

/// <summary>
///     Checks if the results of a play step are equivalent to a given list of mutations. The orderings of the mutation
///     lists are ignored.
/// </summary>
public class MutationListIsEquivalentConstraint : Constraint
{
    private readonly ReadOnlyCollection<MutationChecker> Constraints;

    public MutationListIsEquivalentConstraint(IEnumerable<NecoPlayfieldMutation> constraints)
    {
        Constraints = constraints.Select(mut => new MutationChecker(mut)).ToList().AsReadOnly();
    }

    public override string Description
        => "< " + string.Join(", ", Constraints.Select(c => c.Mutation.ToString())) + " >";

    protected override string GetStringRepresentation()
    {
        return Description;
    }

    public override ConstraintResult ApplyTo<TActual>(TActual actual)
    {
        if (actual is not IEnumerable<NecoPlayfieldMutation> actualList) {
            throw new("wrong input type");
        }

        // Ensure matching sizes
        if (actualList.Count() != Constraints.Count()) {
            return new(this, actual, ConstraintStatus.Failure);
        }

        // Track items already matched against
        var matchedMutations = new List<NecoPlayfieldMutation>();

        foreach (var constraint in Constraints) {
            var result = actualList
                .Except(matchedMutations)
                .FirstOrDefault(mut => constraint.EqualsMutation(mut));
            if (result is null) {
                return new(this, actual, ConstraintStatus.Failure);
            }

            matchedMutations.Add(result);
        }

        return new(this, actual, ConstraintStatus.Success);
    }
}

public abstract class Has : NUnit.Framework.Has
{
    /// <inheritdoc cref="MutationListHasConstraint" />
    public static MutationListHasConstraint<T> MutationWhere<T>(Func<T, bool> predicate)
        where T : NecoPlayfieldMutation
    {
        return new(predicate);
    }

    /// <inheritdoc cref="MutationListIsEquivalentConstraint" />
    public static MutationListIsEquivalentConstraint EquivalentMutationsTo(
        params NecoPlayfieldMutation[] mutationCheckers)
    {
        return new(mutationCheckers);
    }

    /// <inheritdoc cref="FieldHasContentsConstraint" />
    public static FieldHasContentsConstraint FieldContents(Dictionary<Vector2i, NecoUnit> expected)
    {
        return new(expected);
    }
}

public static class NUnitExt
{
    /// <inheritdoc cref="MutationListHasConstraint" />
    public static MutationListHasConstraint<T> MutationWhere<T>(this ConstraintExpression expr, Func<T, bool> predicate)
        where T : NecoPlayfieldMutation
    {
        var constraint = new MutationListHasConstraint<T>(predicate);
        expr.Append(constraint);
        return constraint;
    }

    /// <inheritdoc cref="MutationListIsEquivalentConstraint" />
    public static MutationListIsEquivalentConstraint EquivalentMutationsTo(
        this ConstraintExpression expr,
        List<NecoPlayfieldMutation> mutationCheckers)
    {
        return new(mutationCheckers);
    }

    /// <inheritdoc cref="FieldHasContentsConstraint" />
    public static FieldHasContentsConstraint HasFieldContents(
        this ConstraintExpression expr,
        Dictionary<Vector2i, NecoUnit> expected)
    {
        var constraint = new FieldHasContentsConstraint(expected);
        expr.Append(constraint);
        return constraint;
    }
}

#endregion
