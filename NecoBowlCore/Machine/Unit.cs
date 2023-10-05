using System.Text.RegularExpressions;
using NecoBowl.Core.Machine;
using NecoBowl.Core.Model;
using NecoBowl.Core.Sport.Tactics;
using NecoBowl.Core.Tags;
using NLog;

namespace NecoBowl.Core.Sport.Play;

public record NecoUnitId
{
    public const int StringLength = 6;

    public static readonly Regex StringIdRegex
        = new($"@U:(?<id>[0-9a-z]{{{StringLength}}})", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public readonly Guid Value = Guid.NewGuid();

    public string ToSimpleString()
    {
        return Value.ToString().Substring(0, StringLength);
    }

    public override string ToString()
    {
        return $"@U:{Value.ToString().Substring(0, StringLength)}";
    }
}

/// <summary>A unit as exists during a play.</summary>
public sealed class Unit : IEquatable<Unit>
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    public readonly Stack<Behavior> ActionStack;
    public readonly string Discriminator;

    public readonly NecoUnitId Id;
    public readonly List<Unit> Inventory = new();
    private readonly List<NecoUnitMod> Mods = new();

    public readonly NecoPlayerId OwnerId;
    public readonly ReactionDict Reactions = new();
    public readonly List<NecoUnitTag> Tags = new();
    public readonly NecoUnitModel UnitModel;
    public Unit? Carrier;
    public int DamageTaken;

    public Unit(NecoUnitModel unitModel, string discriminator, NecoPlayerId ownerId)
    {
        Id = new();

        UnitModel = unitModel;
        Discriminator = discriminator;
        OwnerId = ownerId;
        Tags.AddRange(UnitModel.Tags);
        Reactions.AddRange(unitModel.Reactions);

        ActionStack = new(unitModel.Actions.Reverse());
    }

    public Unit(NecoUnitModel unitModel, NecoPlayerId playerId)
        : this(unitModel, "", playerId)
    {
    }

    internal Unit(NecoUnitModel unitModel)
        : this(unitModel, "", new())
    {
    }

    public bool CanAttackByMovement => !Tags.Contains(NecoUnitTag.Defender);

    public int Power => UnitModel.Power;
    public int MaxHealth => UnitModel.Health;
    public int CurrentHealth => MaxHealth - DamageTaken;

    public int Rotation => (int)(AbsoluteDirection)GetMod<NecoUnitMod.Rotate>().Rotation;

    public AbsoluteDirection Facing => (AbsoluteDirection)Rotation;

    public string FullName => $"{UnitModel.Name}{(Discriminator != string.Empty ? $" {Discriminator}" : "")}";

    public bool Equals(Unit? other)
    {
        return Id.Equals(other?.Id);
    }

    public bool CanAttackOther(Unit other)
    {
        return other.OwnerId != default && OwnerId != other.OwnerId && !Tags.Contains(NecoUnitTag.Defender);
    }

    internal Behavior PopAction()
    {
        if (!ActionStack.Any()) {
            throw new NecoBowlFieldException($"unit {this} has no actions");
        }

        var value = ActionStack.Pop();

        // Refill actions if exhausted.
        if (!ActionStack.Any()) {
            foreach (var a in UnitModel.Actions.Reverse()) {
                ActionStack.Push(a);
            }
        }

        return value;
    }

    public void AddMod(NecoUnitMod mod)
    {
        mod = mod.Update(this);
        Mods.Add(mod);
    }

    public T GetMod<T>() where T : NecoUnitMod, new()
    {
        return Mods.OfType<T>().Any()
            ? (T)Mods.OfType<T>().Aggregate((orig, next) => (T)next.Apply(orig)).Apply(new T())
            : new();
    }

    public List<Unit> GetInventoryTree(bool includeParent = true)
    {
        return new List<Unit> { this }
            .Concat(Inventory.SelectMany(u => u.GetInventoryTree()))
            .Where(u => includeParent || u != this)
            .ToList();
    }

    public override string ToString()
    {
        return $"{UnitModel.Name}@{nameof(Unit)}:{Id.ToSimpleString()}";
    }

    public bool CanPickUp(Unit pairUnit2)
    {
        return Tags.Contains(NecoUnitTag.Carrier) && pairUnit2.Tags.Contains(NecoUnitTag.Item);
    }

    /// <returns>The unit in this unit's inventory that should be used in a hand-off situation, or null if there isn't one.</returns>
    public Unit? HandoffItem()
    {
        return Inventory.Any() ? Inventory.FirstOrDefault() : null;
    }
}
