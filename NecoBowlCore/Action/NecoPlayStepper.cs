using System.Collections.ObjectModel;
using System.ComponentModel.Design.Serialization;
using System.Drawing;
using System.Reflection.Metadata;

using neco_soft.NecoBowlCore;
using neco_soft.NecoBowlCore.Action;
using neco_soft.NecoBowlCore.Model;
using neco_soft.NecoBowlCore.Tactics;
using neco_soft.NecoBowlCore.Tags;

using NLog;
using NLog.LayoutRenderers.Wrappers;

namespace neco_soft.NecoBowlCore.Action;

public class NecoPlayStepper
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public readonly NecoField Field;
    public readonly NecoUnitEventHandler UnitEventHandler = new();

    private readonly NecoPlayStepResolver Resolver;

    public NecoPlayStepper(NecoPlay play)
    {
        Field = play.Field;

        Resolver = new(UnitEventHandler);
    }
    
    public void Step() {
        Dictionary<NecoUnitId, NecoUnitAction> unitActions = new();
        foreach (var (pos, unit) in Field.GetAllUnits()) {
            unitActions[unit.Id] = unit.PopAction();
        }
        
        Resolver.ApplyUnitActions(Field, unitActions);
    }
}

/// <summary>
/// Takes a mutable field and a list of unit actions. Applies those actions to the corresponding units, and then handles
/// any resulting space conflicts or other collisions.
/// </summary>
internal class NecoPlayStepResolver
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly NecoUnitEventHandler UnitEventHandler;
    
    public NecoPlayStepResolver(NecoUnitEventHandler unitEventHandler)
    {
        UnitEventHandler = unitEventHandler;
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

    private IEnumerable<NecoUnitMovement> FindMovementsFromActions(
        NecoField field,
        IDictionary<NecoUnitId, NecoUnitAction> initialActions, 
        out IDictionary<NecoUnitId, NecoUnitActionResult> results)
    {
        List<UnitMovementPossibility> movements = new();
        
        foreach (var (uid, action) in initialActions) {
            var unit = field.GetUnit(uid);
            var result = action.Result(uid, field);

            switch (result.StateChange) {
                case NecoFieldStateChange.UnitTranslated translation: {
                    movements.Add(new(translation.Movement, result));
                    break;
                }
                case NecoFieldStateChange.UnitPushedOther push: {
                    movements.Add(new(push.Pusher, result));
                    movements.Add(new(push.Receiver, result, 1));
                    break;
                }
                default: {
                    var pos = field.GetUnitPosition(uid);
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
        
        results = movements.ToDictionary(m => m.Movement.Unit.Id, m => m.Result);
        return movements.Select(m => m.Movement);
    }

    public void ApplyUnitActions(NecoField field, IDictionary<NecoUnitId, NecoUnitAction> initialActions)
    {
        var movements = FindMovementsFromActions(field, initialActions, out var actionResults).ToList();
        
        // Search through all of the movement events to find any instances of collision or space swap.
        Dictionary<NecoUnit, Vector2i> movementResults = new();
        List<NecoUnit> culledUnits = new();

        void HandleCollision(NecoUnitMovement unit1, NecoUnitMovement unit2)
        {
            var unitPair = new UnitPair(unit1, unit2);
            if (unitPair.UnitsAreEnemies()) {
                // Enemy collision
                var winner = unitPair.GetCombatWinner(out var damageIncurred, out var loser);
                if (winner is not null) {
                    Logger.Debug($"Combat between {unit1.Unit} and {unit2.Unit} (Winner {winner.Unit})");
                    winner.Unit.DamageTaken += damageIncurred; 
                    movementResults[winner.Unit] = winner.NewPos;
                    culledUnits.Add(loser!.Unit);
                } else {
                    Logger.Debug($"Combat between {unit1.Unit} and {unit2.Unit} (No winner)");
                    culledUnits.Add(unit1.Unit);
                    culledUnits.Add(unit2.Unit);
                    // Neither of them gets to move.
                }
            } else {
                if (unit1.NewPos == unit2.OldPos && unit2.NewPos == unit1.OldPos) {
                    // If they're just swapping spaces, force them to do nothing.
                    movementResults[unit1.Unit] = unit1.OldPos;
                    movementResults[unit2.Unit] = unit2.OldPos;
                    return;
                }
                
                var winner = unitPair.Collection.OrderByCombatPriority().First();
                var loser = unitPair.Collection.OrderByCombatPriority().Last();
                
                movementResults[winner.Unit] = winner.NewPos;
                movementResults[loser.Unit] = loser.OldPos;
            } 
        }
        
        IEnumerable<NecoUnitMovement> MovementsWithChanges()
            => movements.ExceptBy(culledUnits, u => u.Unit)
                .Select(m => m with { NewPos = movementResults.GetValueOrDefault(m.Unit, m.NewPos) });

        // Conflict resolution loop.
        do {
            var sortedMovements = MovementsWithChanges()
                .OrderBy(u => u.OldPos.Y)
                .ThenBy(u => u.OldPos.X)
                .ToList();

            // Tracker list for movements that have been acknowledged/handled.
            List<NecoUnitMovement> processedMovements = new();
            IEnumerable<NecoUnitMovement> UnprocessedUnits() => MovementsWithChanges().Except(processedMovements);

            foreach (var movement in sortedMovements) {
                if (processedMovements.Contains(movement)) {
                    Logger.Debug($"Skipping movement processing for {movement.Unit} because it has already been handled");
                    continue;
                }

                // Handle units swapping spaces
                var spaceSwapPartner = UnprocessedUnits().FirstOrDefault(m 
                        => m.OldPos == movement.NewPos && movement.OldPos == m.NewPos && m != movement);
                if (spaceSwapPartner is not null) {
                    Logger.Debug($"Handling space swap between {movement.Unit} and {spaceSwapPartner.Unit}");
                    HandleCollision(movement, spaceSwapPartner);
                    processedMovements.Add(movement);
                    processedMovements.Add(spaceSwapPartner);
                    continue;
                }

                // Handle units moving to the same space
                var spaceConflicts = UnprocessedUnits()
                    .Concat(movementResults.Select(kv => new NecoUnitMovement(kv.Key, kv.Value, kv.Value)))
                    .Where(m => m.NewPos == movement.NewPos && m != movement)
                    .OrderByCombatPriority()
                    .ToList();

                var spaceConflictPartner = spaceConflicts.FirstOrDefault();
                if (spaceConflictPartner != movement && spaceConflictPartner is not null) {
                    // Prioritize fighting enemies
                    var enemy = spaceConflicts.FirstOrDefault(m
                            => m.Unit.OwnerId != movement.Unit.OwnerId && !m.Unit.OwnerId.IsNeutral);
                    spaceConflictPartner = enemy ?? spaceConflictPartner;
                    if (enemy is not null) {
                        Logger.Debug($"Handling enemy space conflict between {movement.Unit} and {enemy.Unit}");
                        HandleCollision(movement, enemy);
                    } else {
                        Logger.Debug(
                            $"Handling friendly space conflict between {movement.Unit} and {spaceConflictPartner.Unit}");
                        HandleCollision(movement, spaceConflictPartner);
                    }

                    processedMovements.Add(movement);
                    processedMovements.Add(spaceConflictPartner);
                    continue;
                }

                // There's no conflict, just apply the move.
                movementResults[movement.Unit] = movement.NewPos;
                processedMovements.Add(movement);
                Logger.Debug($"{movement.Unit} moves to {movement.NewPos}");
            }
        } while (MovementsWithChanges().GroupByFriendlyUnitCollisions().Any());

        // Wipe moved units from the field...
        foreach (var unit in movements) {
            field.GetAndRemoveUnit(unit.OldPos);
            
            // ...and put the surviving units in their new locations.
            if (movementResults.Keys.Contains(unit.Unit)) {
                field[movementResults[unit.Unit]] = field[movementResults[unit.Unit]] with { Unit = unit.Unit };
            } else {
                Logger.Debug($"Culling unit {unit.Unit}");
            }
        }

        // ...and then only put back the winners (in their new locations).
        foreach (var (unit, pos) in movementResults) {
            field[pos] = field[pos] with { Unit = unit };
        }
    }
    
}

public record NecoUnitMovement(NecoUnit Unit, Vector2i NewPos, Vector2i OldPos, int Priority = 0);

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

    /// <summary>
    /// Calculate the winner of a one-on-one combat between the units in this pair.
    /// </summary>
    /// <param name="damageIncurred">The damage dealt to the winning unit. 0 if there is no winner.</param>
    /// <param name="loser">The loser of the combat. Null if there is no winner.</param>
    /// <returns>The winner of the combat. Null if there is no winner.</returns>
    public NecoUnitMovement? GetCombatWinner(out int damageIncurred, out NecoUnitMovement? loser)
    {
        if (Unit1.Unit.Power == Unit2.Unit.Power) {
            // Power tie.
            loser = null;
            damageIncurred = 0;
            
            // Units with defender win here.
            var defenderUnit = UnitWithTag(NecoUnitTag.Defender, out var nonDefenderUnit);
            if (defenderUnit is not null && nonDefenderUnit is not null) {
                return defenderUnit;
            }
            
            return null;
        }

        var winnerOrder = Collection.OrderByDescending(u => u.Unit.Power).ToList();
        damageIncurred = winnerOrder.Last().Unit.Power;
        loser = winnerOrder.Last();
        return winnerOrder.First();
    }
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
}