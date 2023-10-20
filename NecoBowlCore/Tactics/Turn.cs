using NecoBowl.Core.Input;
using NLog;
using CardPlayMap
    = System.Collections.Generic.Dictionary<NecoBowl.Core.Sport.Tactics.NecoPlayerId,
        System.Collections.Generic.List<NecoBowl.Core.Sport.Tactics.Plan.CardPlay>>;

namespace NecoBowl.Core.Sport.Tactics;

/// <summary>
/// Game state container that tracks cards placed by the players. This is consolidated into a <see cref="Plan" /> once
/// the inputs are done being received. Note that in an online scenario, it is probable that inputs from the client's
/// opponent would only be received after the player has submitted their own. Therefore, the <see cref="CardPlays" />
/// dictionary likely only contains populated data for one player until <see cref="Finished" /> is true. <p /> Turns are
/// applied to a <see cref="Push" />.
/// </summary>
internal class Turn
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly List<NecoInput> AcceptedInputs = new();

    private readonly CardPlayMap CardPlays = new();
    public readonly NecoPlayerPair PlayerPair;

    public readonly uint TurnIndex;

    public Turn(uint turnIndex, NecoPlayerPair playerPair)
    {
        TurnIndex = turnIndex;
        PlayerPair = playerPair;

        // Prepare dicts
        foreach (var player in playerPair.Enumerate()) {
            CardPlays[player.Id] = new();
        }
    }

    public bool Finished { get; private set; }
    public uint BaseMoney => 10;

    public IEnumerable<Plan.CardPlay> AllCardPlays
        => CardPlays.Values.Aggregate((orig, next) => orig.Concat(next).ToList());

    public IReadOnlyDictionary<NecoPlayerRole, List<Plan.CardPlay>> CardPlaysByRole
        => CardPlays.ToDictionary(kv => PlayerPair.RoleOf(kv.Key), kv => kv.Value);

    public int RemainingMoney(NecoPlayerRole role)
    {
        return CardPlays[PlayerPair[role].Id].Aggregate((int)BaseMoney, (val, play) => val - play.Card.Cost);
    }

    public void Finish()
    {
        Finished = true;
    }

    /// <summary>
    /// Gets the Turn object that would come after this one. Note that this does not check if this turn is Finished; that is up
    /// to the caller to check if this is being used to progress the game.
    /// </summary>
    public Turn NextTurn()
    {
        return new(TurnIndex + 1, PlayerPair);
    }

    public IEnumerable<NecoInput> GetInputs()
    {
        return AcceptedInputs;
    }

    public NecoInputResponse TakeInput(NecoInput input)
    {
        Logger.Info($"Input: {input}");

        try {
            var resp = ProcessInput(input);
            if (resp.ResponseKind == NecoInputResponse.Kind.Success) {
                AcceptedInputs.Add(input);
            }

            Logger.Info(resp.ResponseKind);

            return resp;
        }
        catch (Exception e) {
            Logger.Error(e.Message + "\n" + e.StackTrace);
            return NecoInputResponse.Error(e);
        }
    }

    private NecoInputResponse ProcessInput(NecoInput input)
    {
        if (Finished) {
            throw new NecoInputException("received input while turn is finished");
        }

        if (PlayerPair.PlayerByIdOrNull(input.PlayerId) is null) {
            throw new NecoInputException($"input had invalid player {input.PlayerId}");
        }

        switch (input) {
            case NecoInput.PlaceCard placeCard:
                return ProcessInput(placeCard);
            case NecoInput.SetPlanMod planMod:
                return ProcessInput(planMod);
            default:
                throw new NecoInputException($"received input of unhandled type {input.GetType()}");
        }
    }

    private NecoInputResponse ProcessInput(NecoInput.PlaceCard input)
    {
        if (input.Card.Cost > RemainingMoney(PlayerPair.RoleOf(input.PlayerId))) {
            return NecoInputResponse.Illegal(
                $"Not enough money (cost {input.Card.Cost}, have {RemainingMoney(PlayerPair.RoleOf(input.PlayerId))})");
        }

        if (AllCardPlays.Any(p => p.Position == input.Position)) {
            return NecoInputResponse.Illegal($"The space {input.Position} is already occupied.");
        }

        // Bail out before placing card
        if (input.DryRun) 
            return NecoInputResponse.Success();
        
        CardPlays[input.PlayerId].Add(new(input.PlayerId, input.Card, input.Position));

        return NecoInputResponse.Success();
    }

    private NecoInputResponse ProcessInput(NecoInput.SetPlanMod input)
    {
        var card = CardPlays.Values.SelectMany(v => v, (l, c) => c).SingleOrDefault(c => c.Card.CardId == input.CardId)?.Card;
        if (card is null) {
            throw new NecoInputException($"card {input.CardId} not found in this turn");
        }

        if (input.DryRun)
            return NecoInputResponse.Success();
        
        card.Options.SetValue(input.OptionIdentifier, input.OptionValue);
        return NecoInputResponse.Success();
    }
}
