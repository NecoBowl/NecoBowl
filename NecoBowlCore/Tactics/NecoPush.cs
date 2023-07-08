using neco_soft.NecoBowlCore.Action;
using neco_soft.NecoBowlCore.Input;

namespace neco_soft.NecoBowlCore.Tactics;

/// <summary>
/// Game state container representing the state of the board as the offense pushes towards the defense.
/// Stores <see cref="NecoPlan"/>s that develop over the course of a number of turns (see <see cref="NecoTurn"/>).
///
/// Pushes are used to create <see cref="NecoPlay"/> to hand off to the Action of the game.
/// </summary>
public class NecoPush
{
    public readonly Dictionary<NecoPlayerRole, NecoPlan> Plans = new();
    public readonly NecoFieldParameters FieldParameters;
    public NecoTurn CurrentTurn { get; private set; }

    private uint TurnIndex = 0;

    public NecoPush(NecoPlayerPair players, NecoFieldParameters fieldParameters)
    {
        FieldParameters = fieldParameters;
        CurrentTurn = new NecoTurn(TurnIndex, players);
        foreach (var r in Enum.GetValues<NecoPlayerRole>()) {
            Plans[r] = new NecoPlan();
        }
    }

    public void AdvanceTurn()
    {
        if (!CurrentTurn.Finished)
            throw new InvalidOperationException("cannot advance turn before finishing the previous one");
        
        CurrentTurn = CurrentTurn.NextTurn(out TurnIndex);
    }

    /// <summary>
    /// Creates a new Play from the cards played during this push. The Play object is self-contained and has no direct effect
    /// on the state of the match.
    /// </summary>
    public NecoPlay CreatePlay()
    {
        var field = CreateField();
        return new NecoPlay(field);
    }

    private NecoField CreateField()
    {
        var field = new NecoField(FieldParameters);
        
        foreach (var (role, plan) in Plans) {
            foreach (var cardPlay in plan.GetCardPlays()) {
                if (cardPlay.Card is NecoUnitCard unitCard) {
                    field[cardPlay.Position] = field[cardPlay.Position] with { Unit = new NecoUnit(unitCard.UnitModel) };
                }
            }
        }

        return field;
    }


    public class PlayerRoleMap : Dictionary<NecoPlayerId, NecoPlayerRole>
    { }
}