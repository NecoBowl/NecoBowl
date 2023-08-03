using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

using neco_soft.NecoBowlCore.Action;
using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlCore.Model;

public abstract class NecoUnitModel
{
    public abstract string InternalName { get; }
    public abstract string Name { get; }
    public abstract int Health { get; }
    public abstract int Power { get; }
    public abstract IEnumerable<NecoUnitTag> Tags { get; }
    public abstract IEnumerable<NecoUnitAction> Actions { get; }
}

/// <summary>
/// Dummy implementation of a UnitModel for testing purposes.
/// </summary>
public class NecoUnitModelCustom : NecoUnitModel
{
    public NecoUnitModelCustom(string name, int health, int power, IReadOnlyCollection<NecoUnitTag> tags, IEnumerable<NecoUnitAction> actions)
        : base()
    {
        Name = name;
        InternalName = name;
        Health = health;
        Power = power;
        Tags = tags;
        Actions = actions;
    }
    
    public NecoUnitModelCustom(string name, int power, IReadOnlyCollection<NecoUnitTag> tags, IEnumerable<NecoUnitAction> actions)
        : this(name, power, power, tags, actions)
    { }

    public override string InternalName { get; }
    public override string Name { get; }
    public override int Health { get; }
    public override int Power { get; }
    public override IReadOnlyCollection<NecoUnitTag> Tags { get; }
    public override IEnumerable<NecoUnitAction> Actions { get; }
    
    public static NecoUnitModelCustom Mover(string name, int health, int power, AbsoluteDirection direction)
    {
        var unit = new NecoUnitModelCustom(
            name,
            health,
            power,
            new NecoUnitTag[] { },
            new[] { new NecoUnitAction.TranslateUnit((RelativeDirection)direction) });

        return unit;
    }

    #region Old Methods (no health field)
    public static NecoUnitModelCustom Mover(string name, int power, AbsoluteDirection direction)
    {
        var unit = new NecoUnitModelCustom(
            name,
            power,
            new NecoUnitTag[] { },
            new[] { new NecoUnitAction.TranslateUnit((RelativeDirection)direction) });

        return unit;
    }
    
    public static NecoUnitModelCustom Pusher(string name, int power, AbsoluteDirection direction)
    {
        var unit = new NecoUnitModelCustom(
            name,
            power,
            new NecoUnitTag[] { NecoUnitTag.Pusher },
            new[] { new NecoUnitAction.TranslateUnit((RelativeDirection)direction) });

        return unit;
    }

    public static NecoUnitModelCustom DoNothing(string name = "DoNothing", int power = 1)
    {
        var unit = new NecoUnitModelCustom(name, power, new NecoUnitTag[] { }, new NecoUnitAction[] { new NecoUnitAction.DoNothing() });
        return unit;
    }

    public static NecoUnitModelCustom Ball(string name = "TestBall", int power = 1)
    {
        return new NecoUnitModelCustom(name,
            power,
            new NecoUnitTag[] { NecoUnitTag.TheBall },
            new NecoUnitAction[] { new NecoUnitAction.DoNothing() });
    }
    #endregion
}

public class NecoUnitModelCustonNew : NecoUnitModel
{
    public NecoUnitModelCustonNew(string name, int health, int power, IReadOnlyCollection<NecoUnitTag> tags, IEnumerable<NecoUnitAction> actions)
    {
        Name = name;
        InternalName = name;
        Health = health;
        Power = power;
        Tags = tags;
        Actions = actions;
    }

    public override string InternalName { get; }
    public override string Name { get; }
    public override int Health { get; }
    public override int Power { get; }
    public override IReadOnlyCollection<NecoUnitTag> Tags { get; }
    public override IEnumerable<NecoUnitAction> Actions { get; }
}

public class NecoUnitPlanModOptionsException : Exception
{
    public NecoUnitPlanModOptionsException() { }
    public NecoUnitPlanModOptionsException(string message) : base(message) { }
    public NecoUnitPlanModOptionsException(string message, Exception inner) : base(message, inner) { }
}

