using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

using neco_soft.NecoBowlCore.Model;
using neco_soft.NecoBowlCore.Tactics;
using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlCore.Action;

public record NecoUnitId()
{
    public readonly Guid Value = Guid.NewGuid();

    public override string ToString()
        => Value.ToString().Substring(0, 6);
};

/// <summary>
/// A unit as exists during a play.
/// </summary>
public sealed class NecoUnit : IEquatable<NecoUnit>
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public readonly NecoUnitId Id;
    
    public readonly NecoPlayerId OwnerId;
    public readonly NecoUnitModel UnitModel;
    public readonly string Discriminator;

    public int Power => UnitModel.Power;
    public int MaxHealth => UnitModel.Health;
    public int CurrentHealth => MaxHealth - DamageTaken;
    public int DamageTaken;
    public readonly Stack<NecoUnitAction> ActionStack;
    public readonly List<NecoUnit> Inventory = new();
    public readonly List<NecoUnitTag> Tags = new();
    public readonly List<NecoUnitMod> Mods = new();

    public int Rotation => GetMod<NecoUnitMod.Rotate>().Rotation;
    public AbsoluteDirection Facing => (AbsoluteDirection)Rotation;

    public string FullName => $"{UnitModel.Name}{(Discriminator != string.Empty ? $" {Discriminator}" : "")}";

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

    internal NecoUnit(NecoUnitModel unitModel)
        : this(unitModel, "", new())
    { }

    internal NecoUnitAction PopAction()
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