using System.Reflection;
using System.Runtime.CompilerServices;

using neco_soft.NecoBowlCore;
using neco_soft.NecoBowlCore.Model;

using NLog;

namespace neco_soft.NecoBowlDefinitions;

public static class NecoDefinitions
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private const string UnitModelNamespace = "neco_soft.NecoBowlDefinitions.Unit";
    private const string CardModelNamespace = "neco_soft.NecoBowlDefinitions.Card";

    public readonly static IReadOnlyList<NecoUnitModel> AllUnitModels;
    public readonly static IReadOnlyList<NecoCardModel> AllCardModels;

    static NecoDefinitions()
    {
        AllUnitModels = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.Namespace == UnitModelNamespace)
            .Select(t => (NecoUnitModel)t.GetField("Instance")!.GetValue(null)!)
            .Append(BuiltInDefinitions.Ball.Instance)
            .ToList();
        
        AllCardModels = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.Namespace == CardModelNamespace)
            .Select(t => (NecoCardModel)t.GetField("Instance")!.GetValue(null)!)
            .ToList();
    }
}