namespace neco_soft.NecoBowlTest;

[TestFixture]
public class GeneralTests
{
    [Test]
    public void Direction_Relative_IncreaseIsClockwise()
    {
        var dir = AbsoluteDirection.North;
        Assert.Multiple(() => {
            Assert.That(dir.RotatedBy(RelativeDirection.Up), Is.EqualTo(AbsoluteDirection.North));
            Assert.That(dir.RotatedBy(RelativeDirection.Right), Is.EqualTo(AbsoluteDirection.East));
        });
    }
}