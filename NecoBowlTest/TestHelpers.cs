using NecoBowl.Core;
using NecoBowl.Core.Input;
using NecoBowl.Core.Machine;
using NecoBowl.Core.Machine.Behaviors;
using NecoBowl.Core.Model;
using NecoBowl.Core.Sport.Play;
using NecoBowl.Core.Sport.Tactics;
using NecoBowl.Core.Tactics;
using NecoBowl.Core.Tags;

namespace neco_soft.NecoBowlTest;

internal static class TestHelpers
{
    public static UnitCard TestCard(int cost = 0)
    {
        return new(CardModelCustom.FromUnitModel(UnitModelCustom.Item(), cost));
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

/// <summary>Dummy implementation of a UnitModel for testing purposes.</summary>
internal class UnitModelCustom : UnitModel
{
    public UnitModelCustom(
        string name,
        int health,
        int power,
        IReadOnlyCollection<NecoUnitTag>? tags = null,
        IEnumerable<BaseBehavior>? actions = null)
    {
        tags ??= new NecoUnitTag[] { };
        actions ??= new BaseBehavior[] { new DoNothing() };

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
    public override IEnumerable<BaseBehavior> Actions { get; }

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
            new[] { new TranslateUnit(direction) });
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
