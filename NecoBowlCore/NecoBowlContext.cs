using NecoBowl.Core.Input;
using NecoBowl.Core.Machine;
using NecoBowl.Core.Reports;
using NecoBowl.Core.Sport.Tactics;
using NLog;
using Plan = NecoBowl.Core.Reports.Plan;

namespace NecoBowl.Core;

/// <summary>
/// Wrapper around a <see cref="Sport.Tactics.Match" /> for user interaction purposes. You can call
/// <see cref="SendInput" /> to interact with the match state.
/// </summary>
public class NecoBowlContext
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly Match Match;

    public NecoBowlContext(NecoPlayerPair playerPair)
    {
        Players = playerPair;
        Match = new(playerPair);
    }

    public NecoPlayerPair Players { get; }

    public NecoFieldParameters FieldParameters => Match.CurrentPush.FieldParameters;

    public INecoPushInformation Push => Match.CurrentPush;

    /// <summary>Sends a user input to the game.</summary>
    /// <exception cref="NecoInputException">
    /// The game is not able to receive inputs, or was unable to handle the given type of
    /// input.
    /// </exception>
    public NecoInputResponse SendInput(NecoInput input)
    {
        Logger.Info($"Input received: {input}");
        return Match.CurrentPush.SendInput(input);
    }

    public PlayerTurn GetTurn(NecoPlayerRole role)
    {
        return new(Players.FromRole(role).Id, Match.CurrentPush.CurrentTurn.CardPlaysByRole[role].Select(p => p.ToReport()));
    }

    public Plan GetPlan(NecoPlayerRole role)
    {
        return new(Match.CurrentPush.Plans[role]);
    }

    public Play GetPlay()
    {
        return new(Match.CurrentPush.CreatePlay(true));
    }

    public void FinishTurn()
    {
        Match.CurrentPush.FinishTurn();
    }

    public void AdvancePush()
    {
        Match.CurrentPush.AdvancePushStage();
    }
}
