using System.Data;
using System.Reflection;

using neco_soft.NecoBowlCore.Input;
using neco_soft.NecoBowlCore.Tags;

using NLog;

namespace neco_soft.NecoBowlCore.Action;

internal class NecoPlayStepperNew
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly NecoField Field;
    private readonly List<NecoPlayfieldMutation.BaseMutation> PendingMutations = new();
    private readonly List<NecoPlayfieldMutation.BaseMutation> PendingMutationsPostProcessing = new();
    
    private readonly Dictionary<NecoUnitId, NecoUnitMovement> Movements = new();

    private readonly List<NecoPlayfieldMutation> MutationHistory = new();

    public NecoPlayStepperNew(NecoField field)
    {
        Field = field;
    }

    public IEnumerable<NecoPlayfieldMutation> Process()
    {
        // Perform the actions of each unit to populate the lists
        foreach (var (pos, unit) in Field.GetAllUnits()) {
            var result = unit.PopAction().Result(unit.Id, Field.AsReadOnly());

            SetMovementFromAction(unit.Id, result);
        }

        // Begin the substep loop
        while (Movements.Any(m => m.Value.IsChange) || PendingMutations.Any()) {
            // First, process mutations that might effect how movement happens.
            // TODO Order the mutations before processing them.
            ConsumeMutations();

            var movementsToIgnore = new List<NecoUnitMovement>();
            var movementsToReset = new List<NecoUnitId>();

            FixOutOfBoundsMovements();
            
            // Then resolve movements, which may create more mutations.
            foreach (var (uid, movement) in Movements.Select(kv => (kv.Key, kv.Value))) {
                // Resolve space swaps first as they are a special case.
                if (UpdatePendingMutationsBySpaceSwap(movement)) {
                    movementsToIgnore.Add(movement);
                }

                // Then figure out if this unit will get its movement canceled due to space conflicts.
                UpdatePendingMutationsBySpaceConflict(movement, movementsToIgnore, out var shouldReset);
                if (shouldReset) {
                    movementsToReset.Add(uid);
                }
            }

            // For any unit that couldn't make its move, reset its destination position to its current position.
            // TODO Add something to the Combat mutation that says whether the space should be taken over.
            var finalMovements = Movements.Select(kv => (kv.Key, kv.Value)).ToList();
            var unitBuffer = new Dictionary<NecoUnitId, NecoUnitMovement>();
            foreach (var (uid, movement) in finalMovements) {
                if (movementsToReset.Contains(uid)) {
                    Movements[uid] = Movements[uid] with { NewPos = Movements[uid].OldPos };
                } else {
                    var unit = Field[movement.OldPos].Unit!;
                    unitBuffer[unit.Id] = Movements[unit.Id];
                    Field[movement.OldPos] = Field[movement.OldPos] with { Unit = null };
                    Movements.Remove(uid);
                }
            }

            // Perform each movement.
            foreach (var (uid, movement) in unitBuffer.Select(kv => (kv.Key, kv.Value))) {
                // Sanity check for collisions that didn't get handled
                var collisionError = unitBuffer.Values.Where(m => m.NewPos == movement.NewPos && m != movement);
                if (collisionError.Any()) {
                    throw new NecoPlayfieldMutationException("collision in the unit buffer");
                }
                
                // Update the unit position
                Field[movement.NewPos] = Field[movement.NewPos] with { Unit = movement.Unit };

                if (movement.IsChange) {
                    // Add the movement mutation. At the moment, this is mutation doesn't
                    //  actually do anything -- it just serves as a notification to the
                    //  client that the movement occurred.
                    MutationHistory.Add(
                        new NecoPlayfieldMutation.MovementMutation(uid, movement.OldPos, movement.NewPos));
                } else if (movement.Source?.ResultKind == NecoUnitActionResult.Kind.Failure) {
                    if (movement.Source.StateChange is NecoUnitActionOutcome.UnitTranslated attemptedTranslation) {
                        // add bump event
                        MutationHistory.Add(new NecoPlayfieldMutation.UnitBumps(uid,
                            attemptedTranslation.Movement.AsDirection()));
                    } else {
                        Logger.Warn($"unknown failure for movement: {movement.Source.StateChange}");
                    }
                }

                // TODO Use the NecoMovementMutation object to auto
            }

            // Add the post-processing now so the next substep can see it
            PendingMutations.AddRange(PendingMutationsPostProcessing);
            PendingMutationsPostProcessing.Clear();
        }

        foreach (var entry in MutationHistory) {
            Logger.Debug(entry);
        }

        return MutationHistory;
    }

    /// <summary>
    /// Performs the effects of each mutation in <see cref="PendingMutations"/>, removing the mutation in the process.
    /// Populates it with the mutations that result from running the current ones.
    /// </summary>
    private void ConsumeMutations()
    {
        var substepContext = new NecoSubstepContext(PendingMutations, Movements.Values.AsEnumerable());
        foreach (var func in NecoPlayfieldMutation.ExecutionOrder) {
            foreach (var mutation in PendingMutations) {
                func.Invoke(mutation, substepContext, Field);
            }
        }

        foreach (var mutation in PendingMutations) {
            MutationHistory.Add(mutation);
        }

        var tempMutations = new List<NecoPlayfieldMutation.BaseMutation>();

        // Prepare the mutations for next run
        foreach (var mutation in PendingMutations) {
            tempMutations.AddRange(mutation.AddMutations(Field.AsReadOnly()));
        }

        // We have processed the pending mutations.
        PendingMutations.Clear();
        PendingMutations.AddRange(tempMutations);
    }

    private void FixOutOfBoundsMovements()
    {
        // Fix the movement data for Movement entries that are moving to invalid locations.
        // The data behind their original attempt will still live on in the Source property.
        foreach (var (id, movement) in Movements.Where(m => m.Value.Source?.ResultKind == NecoUnitActionResult.Kind.Failure)) {
            Movements[id] = movement with { NewPos = movement.OldPos };
        }
    }

    private void UpdatePendingMutationsBySpaceConflict(NecoUnitMovement movement, IEnumerable<NecoUnitMovement> movementsToIgnore, out bool shouldReset)
    {
        IEnumerable<NecoUnitMovement> OtherMovements() => Movements.Values.Where(m => movement != m);
        
        shouldReset = false;
        
        // Find conflicts that would occur if each unit did its movement.
        var conflicts = OtherMovements()
            .Except(movementsToIgnore.Where(m => m.NewPos == movement.NewPos))
            .Where(m => m.NewPos == movement.NewPos)
            .ToList();

        // Figure out what to do with this unit based on each conflict.
        foreach (var conflict in conflicts) {
            var unitPair = new UnitMovementPair(movement, conflict);
            
            // Prioritize combat first.
            if (unitPair.UnitsCanFight() && movement.IsChange) {
                PendingMutations.Add(new NecoPlayfieldMutation.UnitAttacks(movement.UnitId, conflict.UnitId));
                shouldReset = true;
            } else {
                // Pickup 
                if (unitPair.TryGetUnitsBy(
                        m => m.Unit.Tags.Contains(NecoUnitTag.Item),
                        m => m.Unit.Tags.Contains(NecoUnitTag.Carrier),
                        out var itemUnit,
                        out var carrierUnit)) {
                    if (carrierUnit == movement) {
                        // Pickup occurs
                        PendingMutationsPostProcessing.Add(
                            new NecoPlayfieldMutation.UnitPicksUpItem(carrierUnit!.UnitId, itemUnit!.UnitId));
                    }
                }

                // Friendly unit conflict 
//                PendingMutations.Add(new NecoPlayfieldMutation.BumpUnit(movement.UnitId, ((NecoUnitActionOutcome.UnitTranslated)movement.Source!.StateChange!).Movement.AsDirection()));
                if (DecideCollisionWinner(unitPair) != movement)
                    shouldReset = true;
            }
        }
    }

    /// <returns>True if the unit is undergoing a space swap; false otherwise.</returns>
    private bool UpdatePendingMutationsBySpaceSwap(NecoUnitMovement movement)
    {
        IEnumerable<NecoUnitMovement> OtherMovements() => Movements.Values.Where(m => movement != m);

        var swap = OtherMovements()
            .SingleOrDefault(m => m.NewPos == movement.OldPos && m.OldPos == movement.NewPos);

        if (swap is null)
            return false;
        
        var unitPair = new UnitMovementPair(movement, swap);
        if (unitPair.UnitsAreEnemies()) {
            // Space-swapping units can only fight each other; they block the other from accessing a space on
            //  which another fight may be occurring. Therefore we remove the movement here so that it doesn't
            //  get picked up in space conflict processing.
            PendingMutations.Add(new NecoPlayfieldMutation.UnitAttacks(movement.UnitId, swap.UnitId));
        } else {
            // Friendly space swappers will just bump off each other.
            PendingMutations.Add(new NecoPlayfieldMutation.UnitBumps(movement.UnitId, swap.AsDirection()));
        }
        
        return true;
    }

    private void SetMovementFromAction(NecoUnitId uid, NecoUnitActionResult result)
    {
        switch (result) {
            // Cases where we reset movement
            case { StateChange: NecoUnitActionOutcome.NothingHappened }: 
            case { ResultKind: NecoUnitActionResult.Kind.Failure }: {
                var unit = Field.GetUnit(uid, out var pos);
                Movements[uid] = new(unit, pos, pos, result);
                break;
            } 
            
            // Cases where things happen
            case { StateChange: NecoUnitActionOutcome.UnitTranslated translation }: {
                Movements[uid] = translation.Movement;
                break;
            }
        }
    }

    private static NecoUnitMovement? DecideCollisionWinner(UnitMovementPair unitPair)
    {
        if (unitPair.Collection.DistinctBy(u => u.NewPos).Count() > 1) {
            throw new NecoBowlException("units are not colliding");
        }

        /*
         * - Unit that is stationary
         * - Unit that is not holding ball (when other is holding ball)
         * - Unit with highest power
         * - Unit travelling vertically
         * - Unit traveling diagonally
         * - Bounce
         */
        
        // Holding ball
        if (unitPair.TryUnitWhereSingle(
                u => u.Unit.Tags.Contains(NecoUnitTag.TheBall),
                out var ballUnit,
                out var nonBallUnit)) {
            return ballUnit!;
        }

        { // Max power
            var groups = unitPair.Collection.GroupBy(m => m.Unit.Power).ToList();
            if (groups.Count > 1)
                return groups.MaxBy(g => g.First().Unit.Power)!.First();
        }

        { // Vertical
            var groups = unitPair.Collection.GroupBy(m => Math.Abs(m.Difference.Y)).ToList();
            if (groups.Count > 1)
                return groups.MaxBy(g => g.First().Difference.Y)!.First();
        }
        
        { // Diagonal
            if (unitPair.TryUnitWhereSingle(m => Math.Abs(m.Difference.X) + Math.Abs(m.Difference.Y) > 1,
                    out var diagonalMover, out _)) {
                return diagonalMover!;
            }
        }

        return null;
    }
}

internal static class Extension
{
    public static void RemoveUnitPair(this Dictionary<NecoUnitId, NecoUnitMovement> source, UnitMovementPair unitPair)
    {
        source.Remove(unitPair.Movement1.UnitId);
        source.Remove(unitPair.Movement2.UnitId);
    }
}