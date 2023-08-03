using neco_soft.NecoBowlCore.Input;
using neco_soft.NecoBowlCore.Tactics;

namespace neco_soft.NecoBowlTest;

public static class TestHelpers
{
    public static NecoUnitCard TestCard(int cost = 0)
        => new NecoUnitCard(NecoCardModelCustom.FromUnitModel(NecoUnitModelCustom.DoNothing(), cost));

    public static void AssertSendInput(this NecoBowlContext context, NecoInput input, NecoInputResponse.Kind kind = NecoInputResponse.Kind.Success)
    {
        var resp = context.SendInput(input);
        if (resp.ResponseKind == NecoInputResponse.Kind.Error) {
            throw resp.Exception!;
        }
        Assert.That(resp.ResponseKind, Is.EqualTo(kind));
    }

    
}