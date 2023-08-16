using neco_soft.NecoBowlCore.Tactics;

using NUnit.Framework.Constraints;

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

    [Test]
    public void TestCombat()
    {
        var unitA1 = new NecoUnit(NecoUnitModelCustom.Mover("MoverN", 5, 2));
        var unitA2 = new NecoUnit(NecoUnitModelCustom.Mover("MoverW", 5, 2, RelativeDirection.Left));
        Field[0, 0] = new(unitA1);
        Field[0, 1] = new(unitA2);

        var mutations = new Queue<NecoPlayfieldMutation>(Play.Step());
        Assert.That(mutations,
            Has.MutationWhere<NecoPlayfieldMutation.UnitBumps>(mut => mut.Direction == AbsoluteDirection.West));
        Assert.That(mutations,
            Has.MutationWhere<NecoPlayfieldMutation.UnitAttacks>(mut => mut.Attacker == unitA1.Id));
    }
}

#region Helpers

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

public class Has : NUnit.Framework.Has
{
    public static MutationListHasConstraint<T> MutationWhere<T>(Func<T, bool> predicate)
        where T : NecoPlayfieldMutation
    {
        return new(predicate);
    }
}

#endregion
