using neco_soft.NecoBowlCore.Action;
using neco_soft.NecoBowlCore.Tactics;

using NLog;

namespace neco_soft.NecoBowlCore.Input;

/// <summary>
/// Wrapper around a <see cref="NecoMatch"/> for user interaction purposes.
///
/// You can call <see cref="SendInput"/> to interact with the
/// match state.
/// </summary>
public class NecoBowlContext
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    private NecoMatch Match;
    
    public NecoBowlContext(NecoPlayerPair? playerPair = null)
    {
        Match = new NecoMatch(playerPair);
    }

    /// <summary>
    /// Sends a user input to the game.
    /// </summary>
    /// <exception cref="NecoInputException">The game is not able to receive inputs, or was unable to handle the given type of input.</exception>
    public void SendInput(NecoInput input)
    {
        Match.CurrentPush.CurrentTurn.TakeInput(input);
    }

    public ReadOnlyNecoField GetField(bool preview)
    {
        return Match.CurrentPush.CreateField(preview).AsReadOnly();
    }

    public NecoPlay GetPlay()
    {
        return Match.CurrentPush.CreatePlay(true);
    }
}