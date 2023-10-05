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
    public static NecoUnitCard TestCard(int cost = 0)
    {
        return new(NecoCardModelCustom.FromUnitModel(NecoUnitModelCustom_HealthEqualsPower.DoNothing(), cost));
    }

    public static Unit UnitMover(
        RelativeDirection direction = RelativeDirection.Up,
        NecoUnitTag[]? tags = null,
        NecoPlayer? player = null)
    {
        return new(NecoUnitModelCustom.Mover(direction: direction, tags: tags), player?.Id ?? new());
    }

    public static Unit UnitThrower(NecoPlayer player)
    {
        return new(NecoUnitModelCustom.Thrower(), player.Id);
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
public class NecoUnitModelCustom_HealthEqualsPower : NecoUnitModel
{
    public NecoUnitModelCustom_HealthEqualsPower(
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

    public NecoUnitModelCustom_HealthEqualsPower(
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

    public static NecoUnitModelCustom_HealthEqualsPower Mover(
        string name,
        int health,
        int power,
        AbsoluteDirection direction)
    {
        var unit = new NecoUnitModelCustom_HealthEqualsPower(
            name,
            health,
            power,
            new NecoUnitTag[] { },
            new[] { new Behavior.TranslateUnit((RelativeDirection)direction) });

        return unit;
    }

    #region Old Methods (no health field)

    public static NecoUnitModelCustom_HealthEqualsPower Mover(string name, int power, AbsoluteDirection direction)
    {
        var unit = new NecoUnitModelCustom_HealthEqualsPower(
            name,
            power,
            new NecoUnitTag[] { },
            new[] { new Behavior.TranslateUnit((RelativeDirection)direction) });

        return unit;
    }

    public static NecoUnitModelCustom_HealthEqualsPower Pusher(string name, int power, AbsoluteDirection direction)
    {
        var unit = new NecoUnitModelCustom_HealthEqualsPower(
            name,
            power,
            new[] { NecoUnitTag.Pusher },
            new[] { new Behavior.TranslateUnit((RelativeDirection)direction) });

        return unit;
    }

    public static NecoUnitModelCustom_HealthEqualsPower DoNothing(string name = "DoNothing", int power = 1)
    {
        var unit = new NecoUnitModelCustom_HealthEqualsPower(
            name,
            power,
            new NecoUnitTag[] { },
            new Behavior[] { new DoNothing() });
        return unit;
    }

    public static NecoUnitModelCustom_HealthEqualsPower Ball(string name = "TestBall", int power = 1)
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
public class NecoUnitModelCustom : NecoUnitModel
{
    public NecoUnitModelCustom(
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

    public Unit ToUnit(NecoPlayer owner)
    {
        return new(this, owner.Id);
    }

    #region Initializers

    public static NecoUnitModelCustom Mover(
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

    public static NecoUnitModelCustom Item(string name = "Item", int health = 1, int power = 1)
    {
        return new(
            name,
            health,
            power,
            new[] { NecoUnitTag.Item },
            new[] { new DoNothing() });
    }

    public static NecoUnitModelCustom Thrower(string name = "Thrower", int health = 1, int power = 1)
    {
        return new(name, health, power, new[] { NecoUnitTag.Carrier }, new[] { new AutoThrowBall() });
    }

    #endregion
}
