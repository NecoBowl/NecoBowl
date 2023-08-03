using System.Reflection.Metadata;

using neco_soft.NecoBowlCore.Action;
using neco_soft.NecoBowlCore.Tactics;

using NLog;

namespace neco_soft.NecoBowlCore.Input;

/// <summary>
/// Wrapper around a <see cref="NecoMatch"/> for user interaction purposes.
///
/// You can call <see cref="SendInput"/> to interact with the match state.
/// </summary>
public class NecoBowlContext
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    private NecoMatch Match;
    public NecoPlayerPair Players { get; private set; }
    
    public NecoBowlContext(NecoPlayerPair playerPair)
    {
        Players = playerPair;
        Match = new NecoMatch(playerPair);
    }

    /// <summary>
    /// Sends a user input to the game.
    /// </summary>
    /// <exception cref="NecoInputException">The game is not able to receive inputs, or was unable to handle the given type of input.</exception>
    public NecoInputResponse SendInput(NecoInput input)
    {
        Logger.Info($"Input received: {input}");
        return Match.CurrentPush.SendInput(input);
    }

    public NecoPlanInformation GetPlan(NecoPlayerRole role)
        => new(Match.CurrentPush.Plans[role]);

    public NecoTurnInformation GetTurn()
        => new(Match.CurrentPush.CurrentTurn);

    public NecoPlayInformation BeginPlay()
    {
        return new(Match.CurrentPush.CreatePlay(false, true));
    }

    public NecoPlayInformation GetPlayPreview()
    {
        return new(Match.CurrentPush.CreatePlay(true));
    }

    public NecoFieldParameters FieldParameters => Match.CurrentPush.FieldParameters;

    public void FinishTurn()
    {
        Match.CurrentPush.FinishTurn();
    }

    public void AdvancePush()
    {
        Match.CurrentPush.AdvancePushStage();
    }

    public INecoPushInformation Push => Match.CurrentPush;
}