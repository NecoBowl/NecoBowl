using neco_soft.NecoBowlCore.Input;

namespace neco_soft.NecoBowlCore.Tactics;

/// <summary>
/// Game state container that tracks cards placed by the players. This is consolidated into a <see cref="NecoPlan"/> once
/// the inputs are done being received. Note that in an online scenario, it is probable that inputs from the client's
/// opponent would only be received after the player has submitted their own. Therefore, the <see cref="CardPlays"/>
/// dictionary likely only contains populated data for one player.
///
/// Turns are applied to a <see cref="NecoPush"/>.
/// </summary>
public class NecoTurn : INecoInputReceiver<NecoInput.PlaceCard>,
                       INecoInputReceiver<NecoInput.SetPlanMod>
{
    public bool Finished { get; private set; } = false;
    
    private readonly uint TurnIndex = 0;
    private uint BaseMoney => 3;

    public int RemainingMoney(NecoPlayerRole role)
        => CardPlays[PlayerPair[role].Id].Aggregate((int)BaseMoney, (val, play) => val - play.Card.Cost);

    public readonly Dictionary<NecoPlayerId, List<NecoPlan.CardPlay>> CardPlays = new();
    public readonly NecoPlayerPair PlayerPair;

    public NecoTurn(uint turnIndex, NecoPlayerPair playerPair)
    {
        TurnIndex = turnIndex;
        PlayerPair = playerPair;

        // Prepare Plays dict
        foreach (var player in playerPair.Enumerate()) {
            CardPlays[player.Id] = new List<NecoPlan.CardPlay>();
        }
    }

    public NecoTurn NextTurn(out uint turnIndex)
    {
        turnIndex = TurnIndex + 1;
        return new NecoTurn(turnIndex, PlayerPair);
    }

    public void ApplyToPush(NecoPush push)
    {
        foreach (var (playerId, plays) in CardPlays) {
            var plan = push.Plans[PlayerPair.RoleOf(playerId)];
            foreach (var play in plays) {
                plan.AddCardPlay(play);
            }
        }
    }

    public void ProcessInput(NecoInput.PlaceCard input)
    {
        CardPlays[input.PlayerId].Add(new(input.Card, input.Position));
    }

    public void ProcessInput(NecoInput.SetPlanMod input)
    {
        input.Card.Options.Add(input.Mod);
    }

}