using NecoBowl.Core;
using NecoBowl.Core.Input;
using NecoBowl.Core.Machine;
using NecoBowl.Core.Model;
using NecoBowl.Core.Sport.Play;
using NecoBowl.Core.Sport.Tactics;
using NecoBowl.Core.Tags;

namespace neco_soft.NecoBowlTest;

public static class TestHelpers
{
    public static UnitCard TestCard(int cost = 0)
    {
        return new(CardModelCustom.FromUnitModel(UnitModelCustomHealthEqualsPower.DoNothing(), cost));
    }

    public static Unit UnitMover(
        RelativeDirection direction = RelativeDirection.Up,
        NecoUnitTag[]? tags = null,
        Player? player = null)
    {
        return new(UnitModelCustom.Mover(direction: direction, tags: tags), player?.Id ?? new());
    }

    public static Unit UnitThrower(Player player)
    {
        return new(UnitModelCustom.Thrower(), player.Id);
    }

    public static void AssertSendInput(
        this NecoBowlContext context,
        NecoInput input,
        NecoInputResponse.Kind kind = NecoInputResponse.Kind.Success)
    {
        var resp = context.SendInput(input);
        if (resp.ResponseKind == NecoInputResponse.Kind.Error) {
            throw resp.Exception!;
        }

        Assert.That(resp.ResponseKind, Is.EqualTo(kind));
    }
}

/// <summary>Dummy implementation of a UnitModel for testing purposes. Deprecated.</summary>
// ReSharper disable once InconsistentNaming
public class UnitModelCustomHealthEqualsPower : UnitModel
{
    public UnitModelCustomHealthEqualsPower(
        string name,
        int health,
        int power,
        IReadOnlyCollection<NecoUnitTag> tags,
        IEnumerable<Behavior> actions)
    {
        Name = name;
        InternalName = name;
        Health = health;
        Power = power;
        Tags = tags;
        Actions = actions;
    }

    public UnitModelCustomHealthEqualsPower(
        string name,
        int power,
        IReadOnlyCollection<NecoUnitTag> tags,
        IEnumerable<Behavior> actions)
        : this(name, power, power, tags, actions)
    {
    }

    public override string InternalName { get; }
    public override string Name { get; }
    public override int Health { get; }
    public override int Power { get; }
    public override string BehaviorDescription { get; } = "";
    public override IReadOnlyCollection<NecoUnitTag> Tags { get; }
    public override IEnumerable<Behavior> Actions { get; }

    public static UnitModelCustomHealthEqualsPower Mover(
        string name,
        int health,
        int power,
        AbsoluteDirection direction)
    {
        var unit = new UnitModelCustomHealthEqualsPower(
            name,
            health,
            power,
            new NecoUnitTag[] { },
            new[] { new Behavior.TranslateUnit((RelativeDirection)direction) });

        return unit;
    }

    #region Old Methods (no health field)

    public static UnitModelCustomHealthEqualsPower Mover(string name, int power, AbsoluteDirection direction)
    {
        var unit = new UnitModelCustomHealthEqualsPower(
            name,
            power,
            new NecoUnitTag[] { },
            new[] { new Behavior.TranslateUnit((RelativeDirection)direction) });

        return unit;
    }

    public static UnitModelCustomHealthEqualsPower Pusher(string name, int power, AbsoluteDirection direction)
    {
        var unit = new UnitModelCustomHealthEqualsPower(
            name,
            power,
            new[] { NecoUnitTag.Pusher },
            new[] { new Behavior.TranslateUnit((RelativeDirection)direction) });

        return unit;
    }

    public static UnitModelCustomHealthEqualsPower DoNothing(string name = "DoNothing", int power = 1)
    {
        var unit = new UnitModelCustomHealthEqualsPower(
            name,
            power,
            new NecoUnitTag[] { },
            new Behavior[] { new DoNothing() });
        return unit;
    }

    public static UnitModelCustomHealthEqualsPower Ball(string name = "TestBall", int power = 1)
    {
        return new(
            name,
            power,
            new[] { NecoUnitTag.TheBall },
            new Behavior[] { new DoNothing() });
    }

    #endregion
}

/// <summary>Dummy implementation of a UnitModel for testing purposes.</summary>
public class UnitModelCustom : UnitModel
{
    public UnitModelCustom(
        string name,
        int health,
        int power,
        IReadOnlyCollection<NecoUnitTag>? tags = null,
        IEnumerable<Behavior>? actions = null)
    {
        tags ??= new NecoUnitTag[] { };
        actions ??= new Behavior[] { new DoNothing() };

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
    public override string BehaviorDescription { get; } = "";
    public override IReadOnlyCollection<NecoUnitTag> Tags { get; }
    public override IEnumerable<Behavior> Actions { get; }

    public Unit ToUnit(Player owner)
    {
        return new(this, owner.Id);
    }

    #region Initializers

    public static UnitModelCustom Mover(
        string? name = null,
        int health = 1,
        int power = 1,
        RelativeDirection direction = RelativeDirection.Up,
        NecoUnitTag[]? tags = null)
    {
        name ??= $"Mover_{direction.ToString()}";
        tags ??= new NecoUnitTag[] { };
        return new(
            name,
            health,
            power,
            tags,
            new[] { new Behavior.TranslateUnit(direction) });
    }

    public static UnitModelCustom Item(string name = "Item", int health = 1, int power = 1)
    {
        return new(
            name,
            health,
            power,
            new[] { NecoUnitTag.Item },
            new[] { new DoNothing() });
    }

    public static UnitModelCustom Thrower(string name = "Thrower", int health = 1, int power = 1)
    {
        return new(name, health, power, new[] { NecoUnitTag.Carrier }, new[] { new AutoThrowBall() });
    }

    #endregion
}
