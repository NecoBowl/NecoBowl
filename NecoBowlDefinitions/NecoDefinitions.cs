using System.Reflection;
using NecoBowl.Core;
using NecoBowl.Core.Model;
using NLog;

namespace neco_soft.NecoBowlDefinitions;

public static class NecoDefinitions
{
    private const string UnitModelNamespace = "neco_soft.NecoBowlDefinitions.Unit";
    private const string CardModelNamespace = "neco_soft.NecoBowlDefinitions.Card";
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public static readonly IReadOnlyList<UnitModel> AllUnitModels;
    public static readonly IReadOnlyList<CardModel> AllCardModels;

    static NecoDefinitions()
    {
        AllUnitModels = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => (t.Namespace == UnitModelNamespace) & !t.FullName!.Contains("+"))
            .Select(
                t => {
                    Logger.Info(t);
                    return t;
                })
            .Select(t => (UnitModel)t.GetField("Instance")!.GetValue(null)!)
            .Append(BuiltInDefinitions.Ball.Instance)
            .ToList();


        AllCardModels = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.Namespace == CardModelNamespace)
            .Select(t => (CardModel)t.GetField("Instance")!.GetValue(null)!)
            .ToList();
    }
}
