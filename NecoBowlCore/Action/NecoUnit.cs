using System.Text.RegularExpressions;

using neco_soft.NecoBowlCore.Model;
using neco_soft.NecoBowlCore.Tactics;
using neco_soft.NecoBowlCore.Tags;

using NLog;

namespace neco_soft.NecoBowlCore.Action;

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

/// <summary>
///     A unit as exists during a play.
/// </summary>
public sealed class NecoUnit : IEquatable<NecoUnit>
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    public readonly Stack<NecoUnitAction> ActionStack;
    public readonly string Discriminator;

    public readonly NecoUnitId Id;
    public readonly List<NecoUnit> Inventory = new();
    private readonly List<NecoUnitMod> Mods = new();

    public readonly NecoPlayerId OwnerId;
    public readonly ReactionDict Reactions = new();
    public readonly List<NecoUnitTag> Tags = new();
    public readonly NecoUnitModel UnitModel;
    public NecoUnit? Carrier;
    public int DamageTaken;

    public NecoUnit(NecoUnitModel unitModel, string discriminator, NecoPlayerId ownerId)
    {
        Id = new();

        UnitModel = unitModel;
        Discriminator = discriminator;
        OwnerId = ownerId;
        Tags.AddRange(UnitModel.Tags);
        Reactions.AddRange(unitModel.Reactions);

        ActionStack = new(unitModel.Actions.Reverse());
    }

    public NecoUnit(NecoUnitModel unitModel, NecoPlayerId playerId)
        : this(unitModel, "", playerId)
    { }

    internal NecoUnit(NecoUnitModel unitModel)
        : this(unitModel, "", new())
    { }

    public bool CanAttackByMovement => !Tags.Contains(NecoUnitTag.Defender);

    public int Power => UnitModel.Power;
    public int MaxHealth => UnitModel.Health;
    public int CurrentHealth => MaxHealth - DamageTaken;

    public int Rotation => (int)(AbsoluteDirection)GetMod<NecoUnitMod.Rotate>().Rotation;

    public AbsoluteDirection Facing => (AbsoluteDirection)Rotation;

    public string FullName => $"{UnitModel.Name}{(Discriminator != string.Empty ? $" {Discriminator}" : "")}";

    public bool Equals(NecoUnit? other)
    {
        return Id.Equals(other?.Id);
    }

    internal NecoUnitAction PopAction()
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

    public List<NecoUnit> GetInventoryTree(bool includeParent = true)
    {
        return new List<NecoUnit> { this }
            .Concat(Inventory.SelectMany(u => u.GetInventoryTree()))
            .Where(u => includeParent || u != this)
            .ToList();
    }

    public override string ToString()
    {
        return $"{UnitModel.Name}@{nameof(NecoUnit)}:{Id.ToSimpleString()}";
    }

    public bool CanPickUp(NecoUnit pairUnit2)
    {
        return Tags.Contains(NecoUnitTag.Carrier) && pairUnit2.Tags.Contains(NecoUnitTag.Item);
    }
}
