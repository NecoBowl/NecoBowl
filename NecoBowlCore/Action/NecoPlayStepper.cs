using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using neco_soft.NecoBowlCore.Tags;

using NLog;

namespace neco_soft.NecoBowlCore.Action;

/// <summary>
/// </summary>
internal class NecoPlayStepper
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public uint StepCount = 0;

    private readonly NecoField Field;

    public NecoPlayStepper(NecoField field)
    {
        Field = field;
    }

    public IEnumerable<NecoPlayfieldMutation> ApplyPlayStep()
    {
        var resolver = new NecoPlayStepResolver(Field.AsReadOnly());
        
        // Perform the step
        Dictionary<NecoUnitId, NecoUnitAction> unitActions = new();
        foreach (var (_, unit) in Field.GetAllUnits()) {
            unitActions[unit.Id] = unit.PopAction();
        }

        var movements = resolver.FindMovementsFromActions(unitActions).ToList();
        var mutations = resolver.FieldMutationsNew(movements).ToList();
        
        // Step 1
        foreach (var mut in mutations) {
            mut.Pass1Mutate(Field);
        }
        
        // Step 2
        foreach (var mut in mutations) {
            mut.Pass2Mutate(Field);
        }

        StepCount++;

        return mutations;
    }
}

/// <summary>
/// Provides methods to find the outcome of a given set of actions on a field image.
/// </summary>
/// <remarks>
/// This class does not perform any modifications on its field. It only looks at the state of the field and determines
/// the outcome of a step. It is up to the caller to apply these inputs to the field.
/// </remarks>
internal class NecoPlayStepResolver
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly ReadOnlyNecoField Field;
    
    public NecoPlayStepResolver(ReadOnlyNecoField field)
    {
        Field = field;
    }

    private record UnitMovementPossibility
    {
        public readonly NecoUnitMovement Movement;
        public readonly NecoUnitActionResult Result;
        public readonly int Priority;

        public UnitMovementPossibility(NecoUnitMovement movement, NecoUnitActionResult result, int priority = 0)
        {
            Movement = movement with { };
            Result = result;
            Priority = priority;
        }
    }

    /*
     * UNIT PRIORITY:
     * 1. Unit with highest power
     * 2. Unit with lowest Y-coordinate
     * 3. Unit with lowest X-coordinate
     *
     * The winning unit is calculated according to the following process:
     *
     * 1. Pick a unit `u1` that is the highest-priority unit on any team.
     * 2. Pick a unit `u2` that is the highest-priority unit on a team that isn't the team of `u1`.
     * 3. If `u2` exists, perform combat between `u1` and `u2` and return to step 1.
     * 4. The winning unit is `u1`.
     */

    /// <summary>
    /// Find the movement that each unit on the field wants to make.
    /// </summary>
    public IEnumerable<NecoUnitMovement> FindMovementsFromActions(IDictionary<NecoUnitId, NecoUnitAction> initialActions)
    {
        // TODO Sort the keys by combat priority
        
        List<UnitMovementPossibility> movements = new();
        
        foreach (var (uid, action) in initialActions) {
            var unit = Field.GetUnit(uid);
            var result = action.Result(uid, Field);
            
            switch (result.StateChange) {
                case NecoUnitActionOutcome.UnitTranslated translation: {
                    movements.Add(new(translation.Movement, result));
                    break;
                }
                case NecoUnitActionOutcome.UnitPushedOther push: {
                    movements.Add(new(push.Pusher, result));
                    movements.Add(new(push.Receiver, result, 1));
                    break;
                }
                default: {
                    var pos = Field.GetUnitPosition(uid);
                    movements.Add(new(new(unit, pos, pos), result));
                    break;
                }
            }
        }

        // Filter duplicate entries for the same unit, prioritizing those with the highest Priority.
        movements = movements.OrderByDescending(m => m.Priority)
            .GroupBy(m => m.Movement.Unit)
            .Select(g => g.First())
            .ToList();
        
        return movements.Select(m => m.Movement);
    }

    public IEnumerable<NecoPlayfieldMutation> FieldMutationsNew(IEnumerable<NecoUnitMovement> initialMovements)
    {
        var movements = initialMovements.OrderByCombatPriority().ToList();
        var mutations = new List<NecoPlayfieldMutation>();
        var handledCombatPairs = new List<UnitPair>();

        foreach (var (pos, unit) in Field.GetAllUnits()) {
            if (movements.All(m => m.Unit.Id != unit.Id)) {
                movements.Add(new(unit, pos, pos));
            }
        }

        // All movements except for those of units that have died this step.
        IEnumerable<NecoUnitMovement> ActiveMovements()
            => movements!
                .ExceptBy(mutations!.OfType<NecoPlayfieldMutation.KillUnit>().Select(mut => mut.Subject), m => m.Unit.Id);
        
        void ResetUnitMovement(NecoUnitMovement movement)
        {
            movements.Remove(movement);
            movements.Add(movement with { NewPos = movement.OldPos });
        }

        int GetUnitHealthAtEndOfStep(NecoUnit unit)
        {
            return mutations!
                .OfType<NecoPlayfieldMutation.DamageUnit>()
                .Where(mut => mut.Subject == unit.Id)
                .Aggregate(unit.CurrentHealth, (health, mut) => health - (int)mut.DamageAmount);
        }

        void HandleCombat(UnitPair unitPair)
        {
            if (handledCombatPairs.Any(p => p.IsSameUnitsAs(unitPair))) {
                return;
            }
            
            var unit1 = unitPair.Unit1;
            var unit2 = unitPair.Unit2;
            
            mutations.Add(new NecoPlayfieldMutation.FightUnits(unit1.UnitId, unit2.UnitId));
            mutations.Add(new NecoPlayfieldMutation.DamageUnit(unit1.Unit.Id, (uint)unit2.Unit.Power));
            mutations.Add(new NecoPlayfieldMutation.DamageUnit(unit2.Unit.Id, (uint)unit1.Unit.Power));

            var survivingUnits = unitPair.Collection.Where(u => GetUnitHealthAtEndOfStep(u.Unit) > 0);
            if (survivingUnits.Count() == 2) {
                // Both units survived
                ResetUnitMovement(unit1);
                ResetUnitMovement(unit2);
            }

            foreach (var unit in unitPair.Collection) {
                // Skip this if the unit has already been marked dead.
                if (mutations.OfType<NecoPlayfieldMutation.KillUnit>().Any(mut => mut.Subject == unit.Unit.Id)) {
                    continue;
                }
                
                if (GetUnitHealthAtEndOfStep(unit.Unit) <= 0) {
                    ResetUnitMovement(unit);
                    mutations.Add(new NecoPlayfieldMutation.KillUnit(unit.UnitId));
                }
            }
            
            handledCombatPairs.Add(unitPair);
        }

        while (ActiveMovements().GroupBySpaceSwaps().Any()) {
            var swap = ActiveMovements().GroupBySpaceSwaps().First();
            if (swap.UnitsAreEnemies()) {
                HandleCombat(swap);
            } else {
                mutations.Add(new NecoPlayfieldMutation.BumpUnits(swap.Unit1.UnitId, swap.Unit2.UnitId));
                ResetUnitMovement(swap.Unit1);
                ResetUnitMovement(swap.Unit2);
            }
        }

        while (ActiveMovements().GroupByCollisions().Any()) {
            var collision = ActiveMovements().GroupByCollisions().First();
            
            var unit1 = collision[0];
            var unit2 = collision[1];
            var unitPair = new UnitPair(unit1, unit2);

            if (unitPair.UnitsAreEnemies()) {
                HandleCombat(unitPair);
            } else {
                // Friendly conflict; give the movement to the highest-priority unit
                var loser = unitPair.Collection.OrderByCombatPriority().Last();
                if (loser.NewPos == loser.OldPos) {
                    // Reset the winner instead (?)
                    var winner = unitPair.Collection.OrderByCombatPriority().First();
                    ResetUnitMovement(winner);
                } else {
                    ResetUnitMovement(loser);
                }
            }
        }

        // Create a new Move or Push mutation for any unit that is still alive and also has a real move.
        var finalMutations = mutations.Concat(
            movements
            .Where(m
                => mutations.OfType<NecoPlayfieldMutation.KillUnit>().All(mut => mut.Subject != m.Unit.Id))
            .Where(m => m.OldPos != m.NewPos)
            .Select(m => m.ToPlayfieldMutation())
        );

        return finalMutations;
    }
}

/// <summary>
/// Record container for a unit that is moving to another space.
/// </summary>
public record NecoUnitMovement
{
    internal readonly NecoUnit Unit;
    public Vector2i NewPos;
    public Vector2i OldPos;
    
    // Hack to let the end of step calculation see what initiated the movements (push vs manual move).
    internal readonly NecoUnitActionResult? Source;

    public NecoUnitMovement(NecoUnit unit, Vector2i newPos, Vector2i oldPos, NecoUnitActionResult? source = null)
    {
        Unit = unit;
        NewPos = newPos;
        OldPos = oldPos;
        Source = source;
    }

    public NecoUnitId UnitId => Unit.Id;

    internal NecoPlayfieldMutation ToPlayfieldMutation()
        => Source?.StateChange switch {
            NecoUnitActionOutcome.UnitPushedOther pushed => new NecoPlayfieldMutation.PushUnit(Unit.Id, OldPos, NewPos, pushed.Pusher.UnitId),
            _ => new NecoPlayfieldMutation.MoveUnit(Unit.Id, OldPos, NewPos)
        };
}

internal record UnitPair(NecoUnitMovement Unit1, NecoUnitMovement Unit2)
{
    public readonly ReadOnlyCollection<NecoUnitMovement> Collection = new(new[] { Unit1, Unit2 });

    /// <summary>Finds the unit in the pair with the specified tag.</summary>
    /// <param name="tag">The tag to search for.</param>
    /// <param name="other">The unit in the pair that does not have the tag. Null if both units have the tag.</param>
    /// <returns>The first unit in the pair that has the tag, or null if neither unit has it.</returns>
    public NecoUnitMovement? UnitWithTag(NecoUnitTag tag, out NecoUnitMovement? other)
    {
        other = Collection.LastOrDefault(u => !u.Unit.UnitModel.Tags.Contains(tag));
        return Collection.FirstOrDefault(u => u.Unit.UnitModel.Tags.Contains(tag));
    }

    public bool UnitsAreEnemies()
        => Unit1.Unit.OwnerId != default && Unit2.Unit.OwnerId != default && Unit1.Unit.OwnerId != Unit2.Unit.OwnerId;

    public bool IsSameUnitsAs(UnitPair other)
        => (Unit1 == other.Unit1 && Unit2 == other.Unit2) || (Unit2 == other.Unit1 && Unit1 == other.Unit2);
}

internal static class PlayStepperExt
{
    public static IEnumerable<NecoUnitMovement> OrderByCombatPriority(this IEnumerable<NecoUnitMovement> units)
    {
        return units.OrderByDescending(u => u.Unit.Power)
            .ThenBy(u => u.OldPos.Y)
            .ThenBy(u => u.OldPos.X);
    }

    public static IEnumerable<NecoUnitMovement[]> GroupByFriendlyUnitCollisions(
        this IEnumerable<NecoUnitMovement> units)
    {
        return units.GroupBy(u => (u.NewPos, u.Unit.OwnerId))
            .Where(g => g.Count() > 1)
            .Select(g => g.ToArray());
    }

    public static IEnumerable<NecoUnitMovement[]> GroupByCollisions(
        this IEnumerable<NecoUnitMovement> units)
    {
        return units.GroupBy(u => u.NewPos)
            .Where(g => g.Count() > 1)
            .Select(g => g.ToArray());
    }

    public static IEnumerable<UnitPair> GroupBySpaceSwaps(this IEnumerable<NecoUnitMovement> movements)
    {
        var movementsTemp = movements.ToList();
        var construct = new List<UnitPair>();
        var usedUnits = new List<NecoUnitMovement>();
        
        foreach (var move in movementsTemp) {
            if (usedUnits.Contains(move)) {
                continue;
            }
            
            var swap = movementsTemp.FirstOrDefault(m 
                => m.NewPos == move.OldPos 
                     && move.NewPos == m.OldPos
                     && move.Unit != m.Unit);
            if (swap is not null) {
                construct.Add(new UnitPair(move, swap)); 
                usedUnits.Add(move);
                usedUnits.Add(swap);
            }
        }

        return construct;
    }
}