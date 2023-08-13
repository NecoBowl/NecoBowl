using neco_soft.NecoBowlDefinitions;

using NLog;

using NUnit.Framework;

namespace neco_soft.NecoBowlTest;

public class DefinitionsTests
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    [Test]
    public void AllUnitModels()
    {
        foreach (var v in NecoDefinitions.AllUnitModels) Logger.Info(v);
    }
}
