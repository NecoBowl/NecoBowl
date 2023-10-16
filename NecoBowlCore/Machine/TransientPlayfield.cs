using System.Collections.Immutable;
using NLog;

namespace NecoBowl.Core.Machine;

/// <summary>Represents the spaces on a playfield. Unlike <see cref="Playfield" />, the spaces can store multiple units.</summary>
internal class TransientPlayfield : ITransientSpaceContentsGetter
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly IImmutableDictionary<Unit, SpaceImmigrantRemoval> Rejections;
    private readonly IImmutableDictionary<Vector2i, Space> Spaces;

    public TransientPlayfield(ReadOnlyPlayfield field, IEnumerable<TransientUnit> movementsEnumerable)
    {
        var movements = movementsEnumerable.ToList();
        var spaces = new Dictionary<Vector2i, Space>();
        var resetReasons = new Dictionary<Unit, SpaceImmigrantRemoval>();

        // Set up transient space array
        for (var i = 0; i < field.GetBounds().X; i++) {
            for (var j = 0; j < field.GetBounds().Y; j++) {
                var pos = new Vector2i(i, j);
                var movementsToHere = movements.Where(m => m.NewPos == pos);
                var movementsFromHere = movements.SingleOrDefault(m => m.OldPos == pos && m.IsChange);
                spaces[pos] = new(pos, movementsToHere, movementsFromHere);
            }
        }

        // Now we begin the space reassignment algorithm.
        // It repeats until every space has no conflict.
        while (spaces.Values.Any(space => space.HasConflict)) {
            // We will apply the changes suggested by GetRemovedImmigrants to this dict.
            var unstableSpaces = spaces.ToDictionary(kv => kv.Key, _ => new HashSet<TransientUnit>());

            foreach (var (pos, space) in spaces) {
                // Iterate through the rejections. For any unit that was rejected, we put its entry in the `unstableSpaces`
                //  slot corresponding to its *original* position. If the unit was removed, we don't add anything to
                //  unstableSpaces.
                foreach (var removal in space.Rejections) {
                    var movement = removal.Movement.WithoutMovement();
                    if (resetReasons.Keys.Any(u => u == movement.Unit)) {
                        Logger.Warn($"Multiple reset reasons for {removal.Movement.Unit}");
                    }

                    resetReasons[movement.Unit] = removal;

                    // If the item was removed by Removed, we stop before registering it as on-field.
                    if (removal.Reason.IsRemoval) {
                        continue;
                    }

                    // Attempt to place the unit on the field.
                    unstableSpaces[movement.OldPos].Add(movement);
                }

                if (space.Winners is { } && !unstableSpaces[pos].Contains(space.Winners.Winner)) {
                    unstableSpaces[pos].Add(space.Winners.Winner);
                }
            }

            var unstableUnits = unstableSpaces.Values.SelectMany(l => l, (l, u) => u);
            var unhandledEmigrants =
                movements.Where(m => !unstableUnits.Contains(m) && !resetReasons.ContainsKey(m.Unit));
            spaces = unstableSpaces.ToDictionary(
                kv => kv.Key,
                kv => new Space(
                    kv.Key, unstableSpaces[kv.Key],
                    unhandledEmigrants.SingleOrDefault(m => m.OldPos == kv.Key && m.IsChange)));
        }

        // User can access the results via Spaces.
        Spaces = spaces.ToImmutableDictionary();
        Rejections = resetReasons.ToImmutableDictionary();
    }

    public IReadOnlyCollection<TransientUnit> GetMovementsFrom(Vector2i pos)
    {
        return GetAllMovements().Where(m => m.OldPos == pos).ToList();
    }

    public IReadOnlyDictionary<Unit, SpaceImmigrantRemoval> GetRejections()
    {
        return Rejections;
    }

    private IEnumerable<TransientUnit> GetAllMovements()
    {
        return Spaces.SelectMany(kv => kv.Value.GetAllImmigrants(), (kv, movement) => movement);
    }

    /// <summary>
    /// <p>
    /// A space during movement calculation. Stores multiple "immigrants", which are the units attempting to move onto this
    /// space. Upon creation, <see cref="Rejections" /> is populated with a list of removal objects that denote the reason that
    /// unit cannot win the space.
    /// </p>
    /// </summary>
    public class Space
    {
        private readonly TransientUnit? Emigrant;
        private readonly IReadOnlyList<TransientUnit> Immigrants;
        private readonly Vector2i Position;
        public readonly ImmutableList<SpaceImmigrantRemoval> Rejections;
        public readonly WinnerList? Winners;

        public Space(Vector2i position, IEnumerable<TransientUnit> immigrants, TransientUnit? emigrant)
        {
            Immigrants = immigrants.ToList().AsReadOnly();
            Emigrant = emigrant;
            Position = position;

            if (Immigrants.Any(movement => movement.NewPos != Position)) {
                throw new InvalidOperationException("movement destination differs from space position");
            }

            if (Emigrant is { }) {
                if (Emigrant.OldPos != Position) {
                    throw new InvalidOperationException("emigrant source position differs from given position");
                }

                if (!Emigrant.IsChange) {
                    throw new ArgumentException("emigrant must be moving");
                }
            }

            Rejections = GetRemovedImmigrants(out Winners).ToImmutableList();
        }

        public bool HasConflict => Immigrants.Count > 1 || Emigrant is { };

        public IReadOnlyCollection<TransientUnit> GetAllImmigrants()
        {
            return Immigrants;
        }

        /// <summary>
        /// Get all the units that must be removed from this space before it can be turned back into a <see cref="Playfield" />
        /// space.
        /// </summary>
        private IReadOnlyCollection<SpaceImmigrantRemoval> GetRemovedImmigrants(
            out WinnerList? winners)
        {
            var immigrants = Immigrants.ToList();
            var combats = new Dictionary<Unit, List<TransientUnit>>();
            var bestVictoryLevels =
                new Dictionary<TransientUnit, PlayfieldCollisionResolver.CollisionVictoryReasonLevel>();

            // The member of `immigrants` that is performing a space swap with the Emigrant of this space.
            TransientUnit? emigrantSwapper = null;

            if (Emigrant is { }) {
                // Perform space swap, and treat the emigrant as a stationary unit if it is placed back here as a reuslt
                var swapper = immigrants.SingleOrDefault(m => new UnitMovementPair(m, Emigrant).IsSpaceSwap());
                if (swapper is { }) {
                    emigrantSwapper = swapper;
                    var spaceSwapPair = new UnitMovementPair(Emigrant, swapper);
                    if (swapper.Unit.CanAttackOther(Emigrant.Unit) && spaceSwapPair.UnitsAreEnemies()) {
                        combats[swapper.Unit] = new() { Emigrant };
                    }
                    else {
                        immigrants.Add(Emigrant.WithoutMovement());
                        bestVictoryLevels[Emigrant.WithoutMovement()] =
                            PlayfieldCollisionResolver.CollisionVictoryReasonLevel.Stationary;
                    }
                }
            }

            // Iterate through all possible unit pairs and add combat or space victory entries.
            var combinations = PlayfieldCollisionResolver.GetPairCombinations(immigrants).ToList();

            foreach (var pair in combinations) {
                foreach (var movement in pair) {
                    if (movement.Unit.CanAttackOther(pair.OtherMovement(movement).Unit) && pair.UnitsAreEnemies()) {
                        if (!combats.ContainsKey(movement.Unit)) {
                            combats[movement.Unit] = new();
                        }

                        combats[movement.Unit].Add(pair.OtherMovement(movement));
                    }
                }

                foreach (var (movement, level) in PlayfieldCollisionResolver.GetCollisionScores(pair)) {
                    if (movement.NewPos != Position) {
                        continue;
                    }

                    if ((int)bestVictoryLevels.GetValueOrDefault(
                            movement, PlayfieldCollisionResolver.CollisionVictoryReasonLevel.Bounce) >= (int)level) {
                        bestVictoryLevels[movement] = level;
                    }
                }
            }

            if (combinations.Count < 1 && Immigrants.Count > 0 && emigrantSwapper is null) {
                // There is only one immigrant and it's not in a space swap
                bestVictoryLevels[Immigrants.Single()] =
                    PlayfieldCollisionResolver.CollisionVictoryReasonLevel.Stationary;
            }

            winners = null;
            var removals = new List<SpaceImmigrantRemoval>();

            if (!combats.Any() && !bestVictoryLevels.Any()) {
                return removals;
            }

            var victoryLevelGroupings = bestVictoryLevels
                .GroupBy(kv => (int)kv.Value, kv => kv.Key)
                .OrderBy(g => g.Key);
            winners = WinnerList.FromWinnerGroups(victoryLevelGroupings); 

            // Create a removal reason for each unit not on the final space
            foreach (var immigrant in immigrants) {
                if (immigrant == Winners?.Winner && immigrant != emigrantSwapper) {
                    continue;
                }

                if (combats.TryGetValue(immigrant.Unit, out var targets)) {
                    removals.Add(new(immigrant, new SpaceImmigrantRemovalReason.Combat(targets)));
                }
                else if (winners?.AuxiliaryWinners.Contains(immigrant) ?? false) {
                    removals.Add(new(immigrant, new SpaceImmigrantRemovalReason.PickedUp(winners.Winner.Unit)));
                }
                else {
                    removals.Add(new(immigrant, new SpaceImmigrantRemovalReason.Superseded()));
                }
            }

            return removals;
        }
    }
}
