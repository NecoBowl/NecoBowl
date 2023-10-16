using System.Collections.Immutable;
using NecoBowl.Core.Input;
using NecoBowl.Core.Machine;
using NecoBowl.Core.Model;

namespace NecoBowl.Core.Sport.Tactics;

/// <summary>
/// Game state container representing the state of the board as the offense pushes towards the defense. Stores a
/// <see cref="Plan" /> for each player that develop over the course of a number of turns (see <see cref="Turn" /> ).
/// Pushes are used to create a <see cref="PlayMachine" /> to hand off to the Action of the game.
/// </summary>
internal class Push : INecoPushInformation
{
    private readonly Dictionary<NecoPlayerId, bool> EndTurnRequested = new();
    public readonly NecoFieldParameters FieldParameters;

    /// <summary>Stores the <see cref="Plan" /> of each player, indexed by the player role.</summary>
    public readonly ImmutableDictionary<NecoPlayerRole, Plan> Plans;

    public Turn CurrentTurn;

    public Push(NecoPlayerPair players, NecoFieldParameters fieldParameters)
    {
        FieldParameters = fieldParameters;
        CurrentTurn = new(0, players);

        Plans = Enum.GetValues<NecoPlayerRole>().ToImmutableDictionary(r => r, r => new Plan());
        EndTurnRequested = Enum.GetValues<NecoPlayerRole>().ToDictionary(r => players[r].Id, _ => false);
    }

    public bool IsTurnFinished => CurrentTurn.Finished;

    public uint CurrentTurnIndex => CurrentTurn.TurnIndex;
    public uint CurrentBaseMoney => CurrentTurn.BaseMoney;

    public int RemainingMoney(NecoPlayerRole role)
    {
        return CurrentTurn.RemainingMoney(role);
    }

    public NecoInputResponse SendInput(NecoInput input)
    {
        if (input is NecoInput.RequestEndTurn endPlayInput) {
            return ProcessInput(endPlayInput);
        }

        return CurrentTurn.TakeInput(input);
    }

    public IEnumerable<NecoInput> GetTurnInputs()
    {
        return CurrentTurn.GetInputs();
    }

    public void FinishTurn()
    {
        CurrentTurn.Finish();

        // Apply the inputs from the turn to the plans.
        foreach (var (role, plan) in Plans) {
            plan.AddCardPlays(CurrentTurn.CardPlaysByRole[role]);
        }
    }

    /// <summary>Advances the state of this push to the next turn.</summary>
    /// <exception cref="InvalidOperationException">
    /// If the current turn has not been finished (see <see cref="Turn.Finished" />
    /// ).
    /// </exception>
    public void AdvancePushStage()
    {
        if (!CurrentTurn.Finished) {
            throw new InvalidOperationException("cannot advance push before finishing the previous one");
        }

        foreach (var (key, _) in EndTurnRequested) {
            EndTurnRequested[key] = false;
        }

        CurrentTurn = CurrentTurn.NextTurn();
    }

    /// <summary></summary>
    /// <seealso cref="CreateField" />
    public PlayMachine CreatePlay(bool preprocessUnits = false)
    {
        var field = CreateField();
        var play = new PlayMachine(field, preprocessUnits: preprocessUnits);
        return play;
    }

    /// <summary>Creates a Field that represents the field state at the start of the play.</summary>
    /// <returns>A Field with its spaces populated with contents corresponding to the cards played in this push.</returns>
    public Playfield CreateField()
    {
        var field = new Playfield(FieldParameters);

        foreach (var (role, plan) in Plans) {
            var plays = plan.GetCardPlays().ToList();

            // Add the cards from the turn in progress
            plays.AddRange(CurrentTurn.CardPlaysByRole[role]);

            foreach (var cardPlay in plays) {
                if (cardPlay.Card.IsUnitCard()) {
                    field[cardPlay.Position] = field[cardPlay.Position] with {
                        Unit = new(((UnitCardModel)cardPlay.Card.CardModel).Model),
                    };
                }
            }
        }

        return field;
    }

    private NecoInputResponse ProcessInput(NecoInput.RequestEndTurn input)
    {
        // TODO Run an extra copy of the play in here that does any meta-board changes

        if (EndTurnRequested[input.PlayerId]) {
            return NecoInputResponse.Illegal("end play already requested by this player");
        }

        EndTurnRequested[input.PlayerId] = true;
        if (EndTurnRequested.All(kv => kv.Value)) {
            CurrentTurn.Finish();
            AdvancePushStage();
        }

        return NecoInputResponse.Success();
    }
}

public interface INecoPushInformation
{
    public uint CurrentTurnIndex { get; }
    public uint CurrentBaseMoney { get; }
    public int RemainingMoney(NecoPlayerRole role);
}
