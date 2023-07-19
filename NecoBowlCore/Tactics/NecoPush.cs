using System.Collections.Immutable;
using System.Collections.ObjectModel;

using neco_soft.NecoBowlCore.Action;

namespace neco_soft.NecoBowlCore.Tactics;

/// <summary>
/// Game state container representing the state of the board as the offense pushes towards the defense.
/// Stores a <see cref="NecoPlan"/> for each player that develop over the course of a number of turns (see <see cref="NecoTurn"/>).
///
/// Pushes are used to create a <see cref="NecoPlay"/> to hand off to the Action of the game.
/// </summary>
public class NecoPush
{
    /// <summary>
    /// Stores the <see cref="NecoPlan"/> of each player, indexed by the player role.
    /// </summary>
    public readonly ImmutableDictionary<NecoPlayerRole, NecoPlan> Plans;
    
    public readonly NecoFieldParameters FieldParameters;
    public NecoTurn CurrentTurn { get; private set; }
    private NecoPlay? TempPlay;

    private uint TurnIndex = 0;

    public NecoPush(NecoPlayerPair players, NecoFieldParameters fieldParameters)
    {
        FieldParameters = fieldParameters;
        CurrentTurn = new NecoTurn(TurnIndex, players);

        Plans = Enum.GetValues<NecoPlayerRole>().ToImmutableDictionary(r => r, r => new NecoPlan());
    }

    public void FinishTurn()
    {
        CurrentTurn.Finish();

        // Apply the inputs from the turn to the plans.
        foreach (var (role, plan) in Plans) {
            plan.AddCardPlays(CurrentTurn.CardPlaysByRole[role]);
        }
    }
    
    public void AdvancePushStage()
    {
        if (!CurrentTurn.Finished)
            throw new InvalidOperationException("cannot advance turn before finishing the previous one");
        
        // Perform the play to deal with any board effects
        TempPlay ??= CreatePlay(false);
        TempPlay.StepToFinish();

        CurrentTurn = CurrentTurn.NextTurn(out TurnIndex);
    }

    /// <summary>
    /// Creates a new Play from the cards played during this push. The Play object is self-contained and has no direct effect
    /// on the state of the match.
    /// </summary>
    /// <seealso cref="CreateField"/>
    public NecoPlay CreatePlay(bool isPreview = false)
    {
        var field = CreateField(isPreview);
        var play = new NecoPlay(field);
        if (!isPreview)
            TempPlay = play;
        return play;
    }

    /// <summary>
    /// Creates a Field that represents the field state at the start of the play.
    /// </summary>
    /// <returns>A Field with its spaces populated with contents corresponding to the cards played in this push.</returns>
    public NecoField CreateField(bool isPreview)
    {
        var field = new NecoField(FieldParameters);

        foreach (var (role, plan) in Plans) {
            var plays = plan.GetCardPlays().ToList();

            if (isPreview) {
                // Add the cards from the turn in progress
                plays.AddRange(CurrentTurn.CardPlaysByRole[role]);
            }

            foreach (var cardPlay in plays) {
                if (cardPlay.Card.IsUnitCard(out var unitCard)) {
                    field[cardPlay.Position] = field[cardPlay.Position] with { Unit = new NecoUnit(unitCard!.UnitModel) };
                }
            }
        }

        return field;
    }

    public class PlayerRoleMap : Dictionary<NecoPlayerId, NecoPlayerRole>
    { }
}