using System.Collections.Immutable;
using NecoBowl.Core.Input;
using NecoBowl.Core.Machine;

namespace NecoBowl.Core.Sport.Tactics;

/// <summary>
/// Game state container representing the state of the board as the offense pushes towards the defense. Stores a
/// <see cref="Plan" /> for each player that develop over the course of a number of turns (see <see cref="Turn" /> ).
/// Pushes are used to create a <see cref="PlayMachine" /> to hand off to the Action of the game.
/// </summary>
internal class Push : INecoPushInformation
{
    private readonly Dictionary<NecoPlayerId, bool> EndPlayRequested = new();
    public readonly NecoFieldParameters FieldParameters;

    /// <summary>Stores the <see cref="Plan" /> of each player, indexed by the player role.</summary>
    public readonly ImmutableDictionary<NecoPlayerRole, Plan> Plans;

    private bool _isPlayFinished;

    public Turn CurrentTurn;
    private PlayMachine? TempPlay;

    public Push(NecoPlayerPair players, NecoFieldParameters fieldParameters)
    {
        FieldParameters = fieldParameters;
        CurrentTurn = new(0, players);

        Plans = Enum.GetValues<NecoPlayerRole>().ToImmutableDictionary(r => r, r => new Plan());
        EndPlayRequested = Enum.GetValues<NecoPlayerRole>().ToDictionary(r => players[r].Id, _ => false);
    }

    public bool IsTurnFinished => CurrentTurn.Finished;

    public bool IsPlayFinished {
        get => TempPlay is { } && _isPlayFinished;
        set {
            if (TempPlay is null) {
                throw new InvalidOperationException("cannot change play state without an active play");
            }

            _isPlayFinished = value;
        }
    }

    public uint CurrentTurnIndex => CurrentTurn.TurnIndex;
    public uint CurrentBaseMoney => CurrentTurn.BaseMoney;

    public int RemainingMoney(NecoPlayerRole role)
    {
        return CurrentTurn.RemainingMoney(role);
    }

    public NecoInputResponse SendInput(NecoInput input)
    {
        if (input is NecoInput.RequestEndPlay endPlayInput) {
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

        if (TempPlay is null || !TempPlay.IsFinished) {
            throw new InvalidOperationException("cannot advance push before running play");
        }

        foreach (var (key, _) in EndPlayRequested) {
            EndPlayRequested[key] = false;
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
                if (cardPlay.Card.IsUnitCard(out var unitCard)) {
                    field[cardPlay.Position] = field[cardPlay.Position] with {
                        Unit = unitCard!.ToUnit(cardPlay.Player),
                    };
                }
            }
        }

        return field;
    }

    private NecoInputResponse ProcessInput(NecoInput.RequestEndPlay input)
    {
        if (TempPlay is null) {
            throw new NecoInputException("cannot end play before play has started");
        }

        if (EndPlayRequested[input.PlayerId]) {
            return NecoInputResponse.Illegal("end play already requested");
        }

        EndPlayRequested[input.PlayerId] = true;
        if (EndPlayRequested.All(kv => kv.Value)) {
            TempPlay.IsFinished = true;
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
