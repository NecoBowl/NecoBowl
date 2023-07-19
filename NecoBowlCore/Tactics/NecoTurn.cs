using neco_soft.NecoBowlCore.Input;

using CardPlayMap = System.Collections.Generic.Dictionary<neco_soft.NecoBowlCore.Tactics.NecoPlayerId, System.Collections.Generic.List<neco_soft.NecoBowlCore.Tactics.NecoPlan.CardPlay>>;

namespace neco_soft.NecoBowlCore.Tactics;

/// <summary>
/// Game state container that tracks cards placed by the players. This is consolidated into a <see cref="NecoPlan"/> once
/// the inputs are done being received. Note that in an online scenario, it is probable that inputs from the client's
/// opponent would only be received after the player has submitted their own. Therefore, the <see cref="CardPlays"/>
/// dictionary likely only contains populated data for one player until <see cref="Finished"/> is true.
///
/// Turns are applied to a <see cref="NecoPush"/>.
/// </summary>
public class NecoTurn
{
    public bool Finished { get; private set; } = false;
    
    private readonly uint TurnIndex = 0;
    private uint BaseMoney => 3;

    public readonly CardPlayMap CardPlays = new();
    public readonly NecoPlayerPair PlayerPair;

    public int RemainingMoney(NecoPlayerRole role)
        => CardPlays[PlayerPair[role].Id].Aggregate((int)BaseMoney, (val, play) => val - play.Card.Cost);

    public IReadOnlyDictionary<NecoPlayerRole, List<NecoPlan.CardPlay>> CardPlaysByRole
        => CardPlays.ToDictionary(kv => PlayerPair.RoleOf(kv.Key), kv => kv.Value);

    public NecoTurn(uint turnIndex, NecoPlayerPair playerPair)
    {
        TurnIndex = turnIndex;
        PlayerPair = playerPair;

        // Prepare Plays dict
        foreach (var player in playerPair.Enumerate()) {
            CardPlays[player.Id] = new List<NecoPlan.CardPlay>();
        }
    }

    private NecoTurn(uint turnIndex, NecoPlayerPair playerPair, CardPlayMap plays)
         : this(turnIndex, playerPair)
    {
        CardPlays = new(plays);
    }

    public void Finish()
    {
        Finished = true;
    }

    /// <summary>
    /// Gets the Turn object that would come after this one. Note that this does not check if this turn is Finished; that is up
    /// to the caller to check if this is being used to progress the game.
    /// </summary>
    public NecoTurn NextTurn(out uint turnIndex)
    {
        turnIndex = TurnIndex + 1;
        return new NecoTurn(turnIndex, PlayerPair, CardPlays);
    }

    public NecoInputResponse TakeInput(NecoInput input)
    {
        try {
            return ProcessInput(input);
        } catch (Exception e) {
            return NecoInputResponse.Error(e);
        }
    }

    private NecoInputResponse ProcessInput(NecoInput input)
    {
        if (Finished) {
            throw new NecoInputException("received input while turn is finished");
        } else if (PlayerPair.PlayerByIdOrNull(input.PlayerId) is null) {
            throw new NecoInputException($"input had invalid player {input.PlayerId}");
        }
        
        switch (input) {
            case NecoInput.PlaceCard placeCard:
                return ProcessInput(placeCard);
                break;
            case NecoInput.SetPlanMod planMod:
                return ProcessInput(planMod);
                break;
            default:
                throw new NecoInputException($"received input of unhandled type {input.GetType()}");
        }
    }

    private NecoInputResponse ProcessInput(NecoInput.PlaceCard input)
    {
        if (input.Card.Cost > RemainingMoney(PlayerPair.RoleOf(input.PlayerId))) {
            return NecoInputResponse.Illegal($"Not enough money (cost {input.Card.Cost}, have {RemainingMoney(PlayerPair.RoleOf(input.PlayerId))})");
        }
        
        CardPlays[input.PlayerId].Add(new(input.Card, input.Position));
        return NecoInputResponse.Success();
    }

    private NecoInputResponse ProcessInput(NecoInput.SetPlanMod input)
    {
        if (!CardPlays.Values.Any(list => list.Any(cp => cp.Card == input.Card))) {
            throw new NecoInputException($"card {input.Card} not found in this turn");
        }

        input.Card.Options.Add(input.Mod);
        return NecoInputResponse.Success();
    }
}