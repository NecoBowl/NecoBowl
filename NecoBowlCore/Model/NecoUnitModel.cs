using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

using neco_soft.NecoBowlCore.Action;
using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlCore.Model;

public abstract class NecoUnitModel
{
    public abstract string Name { get; }
    public abstract int Power { get; }
    public abstract IEnumerable<NecoUnitTag> Tags { get; }
    public abstract IEnumerable<NecoUnitAction> Actions { get; }
    protected abstract IEnumerable<NecoCardOptionPermission> ModPermissions { get; }
}

public class NecoUnitPlanModOptionsException : Exception
{
    public NecoUnitPlanModOptionsException() { }
    public NecoUnitPlanModOptionsException(string message) : base(message) { }
    public NecoUnitPlanModOptionsException(string message, Exception inner) : base(message, inner) { }
}


public class NecoUnitModelCustom : NecoUnitModel
{
    public NecoUnitModelCustom(string name, int power, IReadOnlyCollection<NecoUnitTag> tags, IEnumerable<NecoUnitAction> actions)
        : base()
    {
        Name = name;
        Power = power;
        Tags = tags;
        Actions = actions;
        ModPermissions = new NecoCardOptionPermission[] { };
    }

    public override string Name { get; }
    public override int Power { get; }
    public override IReadOnlyCollection<NecoUnitTag> Tags { get; }
    public override IEnumerable<NecoUnitAction> Actions { get; }
    protected override IEnumerable<NecoCardOptionPermission> ModPermissions { get; }

    public static NecoUnitModelCustom Mover(string name, int power, AbsoluteDirection direction)
    {
        var unit = new NecoUnitModelCustom(
            name,
            power,
            new NecoUnitTag[] { },
            new[] { new NecoUnitAction.TranslateUnit(direction) });

        return unit;
    }
    
    public static NecoUnitModelCustom Pusher(string name, int power, AbsoluteDirection direction)
    {
        var unit = new NecoUnitModelCustom(
            name,
            power,
            new NecoUnitTag[] { NecoUnitTag.Pusher },
            new[] { new NecoUnitAction.TranslateUnit(direction) });

        return unit;
    }

    public static NecoUnitModelCustom DoNothing(string name, int power)
    {
        var unit = new NecoUnitModelCustom(name, power, new NecoUnitTag[] { }, new NecoUnitAction[] { new NecoUnitAction.DoNothing() });
        return unit;
    }
}