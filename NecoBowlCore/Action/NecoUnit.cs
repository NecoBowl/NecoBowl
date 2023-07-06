using System.Collections;
using System.Drawing;
using System.Security.Cryptography.X509Certificates;

using neco_soft.NecoBowlCore.Model;
using neco_soft.NecoBowlCore.Tactics;
using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlCore.Action;

public readonly record struct NecoUnitId()
{
    public readonly Guid Value = Guid.NewGuid();

    public override string ToString()
        => Value.ToString().Substring(0, 6);
};

/// <summary>
/// A unit as exists during a play.
///
/// DESIGN NOTE: PLEASE don't copy this (except for internals purposes).
/// </summary>
public class NecoUnit : IEquatable<NecoUnit>
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public readonly NecoUnitId Id;
    
    public readonly NecoPlayerId OwnerId;
    public readonly NecoUnitModel UnitModel;
    public readonly string Discriminator;

    public int Power => UnitModel.Power;
    public int Health => UnitModel.Power - DamageTaken;
    public int DamageTaken;
    private readonly Stack<NecoUnitAction> ActionStack;
    public readonly List<NecoUnit> Inventory = new();
    public readonly List<NecoUnitTag> Tags = new();
    public readonly List<NecoUnitMod> Mods = new();

    public int Rotation => GetMod<NecoUnitMod.Rotate>().Rotation;

    public NecoUnit(NecoUnitModel unitModel, string discriminator, NecoPlayerId ownerId)
    {
        Id = new();
        
        UnitModel = unitModel;
        Discriminator = discriminator;
        OwnerId = ownerId;
        Tags.AddRange(UnitModel.Tags);

        ActionStack = new Stack<NecoUnitAction>(unitModel.Actions);
    }
    
    public NecoUnit(NecoUnitModel unitModel, NecoPlayerId playerId)
        : this(unitModel, "", playerId)
    { }

    public NecoUnit(NecoUnitModel unitModel)
        : this(unitModel, new())
    { }

    /// <summary>
    /// Creates a new instance that is a copy of another instance.
    /// This should only be used for internal purposes (namely, duplicating fields for rewind).
    /// </summary>
    internal NecoUnit(NecoUnit other)
    {
        Id = other.Id;
        OwnerId = other.OwnerId;
        UnitModel = other.UnitModel;
        Discriminator = other.Discriminator;
        ActionStack = other.ActionStack;
        foreach (var unit in other.Inventory) {
            throw new Exception("can't clone inventories yet");
        }
    }

    public NecoUnitAction PopAction()
    {
        if (!ActionStack.Any()) {
            throw new NecoBowlFieldException($"unit {this} has no actions");
        }
        
        var value = ActionStack.Pop();
        
        // Refill actions if exhausted.
        if (!ActionStack.Any()) {
            foreach (var a in UnitModel.Actions) {
                ActionStack.Push(a);
            }
        }

        return value;
    }

    public T GetMod<T>() where T : NecoUnitMod, new()
    {
        return Mods.Any() ? Mods.OfType<T>().Aggregate((orig, next) => (T)next.Apply(orig)) : new T();
    }

    public bool Equals(NecoUnit? other)
    {
        return Id.Equals(other?.Id);
    }

    public override string ToString()
        => $"{UnitModel.Name}@{nameof(NecoUnit)}:{Id}";
}