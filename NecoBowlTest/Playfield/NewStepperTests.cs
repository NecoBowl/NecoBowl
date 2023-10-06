using System.Collections.ObjectModel;
using NecoBowl.Core;
using NecoBowl.Core.Machine;
using NecoBowl.Core.Machine.Reports;
using NecoBowl.Core.Sport.Play;
using NecoBowl.Core.Sport.Tactics;
using NUnit.Framework.Constraints;
using Unit = NecoBowl.Core.Machine.Unit;

// ReSharper disable AccessToStaticMemberViaDerivedType

namespace neco_soft.NecoBowlTest;

[TestFixture]
public class NewStepperTests
{
    [SetUp]
    public void SetUp()
    {
        Field = NewField();
        PlayMachine = new(Field);
    }

    private const int TestLevel_Action = 0;
    private const int TestLevel_Mutation = 10;
    private const int TestLevel_Field = 20;

    internal static NecoBowl.Core.Machine.Playfield NewField()
    {
        return new(new((5, 5), (0, 0)));
    }

    internal static NecoBowl.Core.Machine.Playfield Field = null!;
    internal static PlayMachine PlayMachine = null!;

    private readonly Player Player1 = new(), Player2 = new();

    private void SetUp_VerticalCollision(out Unit unitA1, out Unit unitA2)
    {
        unitA1 = UnitModelCustom.Mover("MoverN", 5, 2).ToUnit(Player1);
        unitA2 = UnitModelCustom.Mover("MoverS", 5, 2, RelativeDirection.Down).ToUnit(Player2);
        Field[0, 0] = new(unitA1);
        Field[0, 1] = new(unitA2);
    }

    [Order(TestLevel_Action)]
    [Test]
    public void Play_Action_TranslateUnit_MovesUnit()
    {
        var unit = UnitModelCustom.Mover("Mover", direction: RelativeDirection.Up).ToUnit(Player1);
        Field[0, 0] = new(unit);

        var result = PlayMachine.Step();
        Assert.That(
            result, Has.EquivalentMovementsTo(
                new TransientUnit {
                    Unit = unit,
                    OldPos = (0, 0),
                    NewPos = (0, 1),
                }));
    }

    [Test]
    public void Play_UnitCanAttackUnitBumpingIntoWall()
    {
        var unitNorth = UnitModelCustom.Mover("MoverN", 5, 2).ToUnit(Player1);
        var unitWest = UnitModelCustom.Mover("MoverW", 5, 2, RelativeDirection.Left).ToUnit(Player2);
        Field[0, 0] = new(unitNorth);
        Field[0, 1] = new(unitWest);

        var mutations = PlayMachine.Step();
        Assert.That(
            mutations,
            Has.EquivalentMutationsTo(
                new UnitBumps(unitWest.Id, AbsoluteDirection.West),
                new UnitAttacks(
                    unitNorth.Id,
                    unitWest.Id,
                    unitNorth.Power,
                    UnitAttacks.Kind.SpaceConflict,
                    (0, 1)),
                new UnitTakesDamage(unitWest.Id, (uint)unitNorth.Power)));
    }
#if false
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
    public void Play_UnitCanThrowBall()
    {
        var thrower = new NecoUnit(NecoUnitModelCustom.Thrower(), Player1.Id);
        var item = new NecoUnit(NecoUnitModelCustom.Item());
        var receiver = new NecoUnit(NecoUnitModelCustom.Mover(tags: new[] { NecoUnitTag.Carrier }), Player1.Id);

        thrower.Inventory.Add(item);
        item.Carrier = thrower;

        Field[0, 1] = new(thrower);
        Field[0, 3] = new(receiver);

        Play.Step();
        Play.Step();
    }

    [Test]
    public void Play_Clusterfuck()

    {
        var players = new[] { Player1, Player2 };
        foreach (var direction in Enum.GetValues<RelativeDirection>()) {
            var unit = TestHelpers.UnitMover(direction, player: players[(int)direction % 2]);
            Field[(1, 1) - direction.ToVector2i()] = new(unit);
        }

        var ball = new NecoUnit(NecoUnitModelCustom.Item());
        Field[1, 1] = new(ball);

        Assert.That(
            () => {
                Play.Step();
                Play.Step();
                Play.Step();
            }, Throws.Nothing);
    }

    [Test]
    public void Mutation_AttacksCauseUnitsToTakeDamage()
    {
        SetUp_VerticalCollision(out var unit1, out var unit2);

        Play.Step();

        Assert.That(unit1.CurrentHealth, Is.EqualTo(unit1.MaxHealth - unit2.Power));
    }

    [Test]
    public void Field_HandoffHappensOnConcedingSpaceToAlly()
    {
        var pickupper = new NecoUnit(
            NecoUnitModelCustom.Mover(
                "CarrierW",
                5,
                2,
                RelativeDirection.Left,
                new[] { NecoUnitTag.Carrier }),
            Player1.Id);
        var item = new NecoUnit(NecoUnitModelCustom.Item(), NecoPlayer.NeutralPlayer.Id);
        var bossy = new NecoUnit(
            NecoUnitModelCustom.Mover(
                "Bossy",
                5,
                2,
                RelativeDirection.Up,
                new[] { NecoUnitTag.Carrier }),
            Player1.Id);
        Field[2, 2] = new(pickupper);
        Field[1, 2] = new(item);
        Field[0, 0] = new(bossy);

        // Main unit picks up ball
        Play.Step();

        // Handoff happens
        var mutations = Play.Step();
        Assert.That(
            mutations, Has.EquivalentMutationsTo(
                new NecoPlayfieldMutation.MovementMutation(new(bossy, (0, 2), (0, 1))),
                new NecoPlayfieldMutation.UnitBumps(pickupper.Id, AbsoluteDirection.West),
                new NecoPlayfieldMutation.UnitHandsOffItem(pickupper.Id, bossy.Id, item.Id)));

        Play.Step();
    }
#endif

    [Test]
    public void Movement_TrailingUnitsCanMove()
    {
        var unit1 = UnitModelCustom.Mover().ToUnit(Player1);
        var unit2 = UnitModelCustom.Mover().ToUnit(Player1);

        Field[0, 0] = new(unit1);
        Field[0, 1] = new(unit2);

        var muts = PlayMachine.Step();

#if false
        Assert.That(
            muts,
            Has.EquivalentMutationsTo(
                new NecoPlayfieldMutation.MovementMutation(new(unit1, (0, 1), (0, 0))),
                new NecoPlayfieldMutation.MovementMutation(new(unit2, (0, 2), (0, 1)))));
#endif
    }

#if false
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

        Assert.Multiple(
            () => {
                Assert.That(
                    Field, Has.FieldContents(
                        new() {
                            [(1, 2)] = pusher,
                            [(2, 2)] = carrier
                        }));
                Assert.That(carrier.Inventory, Contains.Item(item));
            });
    }

    /// <remarks>
    /// Made this because there was a issue where <see cref="Movement_ItemPushedOntoConflictSpaceGetsPickedUp" /> would work
    /// fine, but if you gave it the Item tag, it would infinite loop.
    /// </remarks>
    [Test]
    public void Movement_BallPushedOntoConflictSpaceGetsPickedUp()
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
        var item = new NecoUnit(BuiltInDefinitions.Ball.Instance, Player1.Id);

        Field[0, 2] = new(pusher);
        Field[2, 1] = new(carrier);
        Field[1, 2] = new(item);

        var mutations = Play.Step().ToList();

        Assert.Multiple(
            () => {
                Assert.That(
                    Field, Has.FieldContents(
                        new() {
                            [(1, 2)] = pusher,
                            [(2, 2)] = carrier
                        }));
                Assert.That(carrier.Inventory, Contains.Item(item));
            });
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
                new NecoPlayfieldMutation.UnitTakesDamage(enemy.Id, (uint)unitA1.Power),
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

    [Test]
    public void Tag_Bossy_CanBumpIntoItem()
    {
        // See ResolveSpaceConflict
        var unit1 = TestHelpers.UnitMover(RelativeDirection.Right, player: Player1);
        var unit2 = TestHelpers.UnitMover(
            RelativeDirection.Up,
            new[] { NecoUnitTag.Bossy },
            Player1);
        var ball = new NecoUnit(NecoUnitModelCustom.Item(), Player1.Id);

        Field[0, 1] = new(unit1);
        Field[1, 0] = new(unit2);
        Field[1, 1] = new(ball);

        Play.Step();
    }
#endif
}

public record MutationChecker
{
    public readonly Mutation Mutation;

    public MutationChecker(Mutation mutation)
    {
        Mutation = mutation;
    }

    public bool EqualsMutation(Mutation mutation)
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
/// Checks that each space coordinate in the given dictionary has its associated contents in a
/// <see cref="NecoBowl.Core.Machine.Playfield" />.
/// </summary>
internal class FieldHasContentsConstraint : Constraint
{
    private readonly Dictionary<Vector2i, Unit> ExpectedContents;

    public FieldHasContentsConstraint(Dictionary<Vector2i, Unit> expectedContents)
    {
        ExpectedContents = expectedContents;
    }

    public override ConstraintResult ApplyTo<TActual>(TActual actual)
    {
        if (actual is not NecoBowl.Core.Machine.Playfield field) {
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

/// <summary>Checks if a mutation list has a mutation that passes a given predicate.</summary>
internal class MutationListHasConstraint : Constraint
{
    // jank
    private readonly Func<object, bool>? Predicate;

    public MutationListHasConstraint()
    {
    }

    public MutationListHasConstraint(Func<object, bool> predicate)
    {
        Predicate = predicate;
    }

    public override ConstraintResult ApplyTo<TActual>(TActual actual)
    {
        if (actual is IEnumerable<Mutation> mutationList) {
            return new(this, actual, mutationList.Any(Predicate!));
        }

        return new(this, actual, ConstraintStatus.Failure);
    }
}

/// <inheritdoc cref="MutationListHasConstraint" />
/// <typeparam name="T">The type of the mutation.</typeparam>
internal class MutationListHasConstraint<T> : MutationListHasConstraint
    where T : Mutation
{
    private readonly Func<T, bool> Predicate;

    public MutationListHasConstraint(Func<T, bool> predicate)
    {
        Predicate = predicate;
    }

    public override string Description => $"[{typeof(T)}]";

    public override ConstraintResult ApplyTo<TActual>(TActual actual)
    {
        if (actual is IEnumerable<Mutation> mutationList) {
            return new(this, actual, mutationList.OfType<T>().Any(Predicate));
        }

        return new(this, actual, ConstraintStatus.Failure);
    }
}

/// <summary>
/// Checks if the results of a play step are equivalent to a given list of mutations. The orderings of the mutation lists
/// are ignored.
/// </summary>
file class StepHasEquivalentMutationsConstraint : Constraint
{
    private readonly ReadOnlyCollection<MutationChecker> Constraints;

    public StepHasEquivalentMutationsConstraint(IEnumerable<Mutation> constraints)
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
        if (actual is not Step stepResult) {
            throw new("wrong input type");
        }

        var mutations = stepResult.GetAllMutations().ToList();

        // Ensure matching sizes
        if (mutations.Count() != Constraints.Count) {
            return new(this, actual, ConstraintStatus.Failure);
        }

        // Track items already matched against
        var matchedMutations = new List<Mutation>();

        foreach (var constraint in Constraints) {
            var result = mutations
                .Except(matchedMutations)
                .FirstOrDefault(mut => constraint.EqualsMutation(mut));
            if (result is null) {
                return new(this, mutations, ConstraintStatus.Failure);
            }

            matchedMutations.Add(result);
        }

        return new(this, mutations, ConstraintStatus.Success);
    }
}

file class StepHasEquivalentMovementsConstraint : Constraint
{
    private readonly ReadOnlyCollection<TransientUnit> Movements;

    public StepHasEquivalentMovementsConstraint(IEnumerable<TransientUnit> movements)
    {
        Movements = movements.ToList().AsReadOnly();
    }

    public override string Description => $"< {string.Join(", ", Movements)} >";

    public override ConstraintResult ApplyTo<TActual>(TActual actual)
    {
        if (actual is not Step stepResult) {
            throw new("wrong input type");
        }

        var movements = stepResult.GetAllMovements().ToList();

        if (!movements.ToHashSet().SetEquals(Movements.Select(Movement.From).ToHashSet())) {
            return new(this, movements, ConstraintStatus.Failure);
        }

        return new(this, movements, ConstraintStatus.Success);
    }
}

abstract file class Has : NUnit.Framework.Has
{
    /// <inheritdoc cref="MutationListHasConstraint" />
    public static MutationListHasConstraint<T> MutationWhere<T>(Func<T, bool> predicate)
        where T : Mutation
    {
        return new(predicate);
    }

    /// <inheritdoc cref="StepHasEquivalentMutationsConstraint" />
    public static StepHasEquivalentMutationsConstraint EquivalentMutationsTo(
        params Mutation[] mutationCheckers)
    {
        return new(mutationCheckers);
    }

    public static StepHasEquivalentMovementsConstraint EquivalentMovementsTo(
        params TransientUnit[] movements)
    {
        return new(movements);
    }

    /// <inheritdoc cref="FieldHasContentsConstraint" />
    public static FieldHasContentsConstraint FieldContents(Dictionary<Vector2i, Unit> expected)
    {
        return new(expected);
    }
}

static file class NUnitExt
{
    /// <inheritdoc cref="MutationListHasConstraint" />
    public static MutationListHasConstraint<T> MutationWhere<T>(this ConstraintExpression expr, Func<T, bool> predicate)
        where T : Mutation
    {
        var constraint = new MutationListHasConstraint<T>(predicate);
        expr.Append(constraint);
        return constraint;
    }

    /// <inheritdoc cref="StepHasEquivalentMutationsConstraint" />
    public static StepHasEquivalentMutationsConstraint EquivalentMutationsTo(
        this ConstraintExpression expr,
        IEnumerable<Mutation> mutationCheckers)
    {
        var constraint = new StepHasEquivalentMutationsConstraint(mutationCheckers);
        expr.Append(constraint);
        return constraint;
    }

    public static StepHasEquivalentMovementsConstraint EquivalentMovementsTo(
        this ConstraintExpression expr,
        IEnumerable<TransientUnit> expected)
    {
        var constraint = new StepHasEquivalentMovementsConstraint(expected);
        expr.Append(constraint);
        return constraint;
    }

    /// <inheritdoc cref="FieldHasContentsConstraint" />
    public static FieldHasContentsConstraint HasFieldContents(
        this ConstraintExpression expr,
        Dictionary<Vector2i, Unit> expected)
    {
        var constraint = new FieldHasContentsConstraint(expected);
        expr.Append(constraint);
        return constraint;
    }
}

#endregion
