using NecoBowl.Core.Input;
using NLog;
using CardPlayMap
    = System.Collections.Generic.Dictionary<NecoBowl.Core.Sport.Tactics.NecoPlayerId,
        System.Collections.Generic.List<NecoBowl.Core.Sport.Tactics.NecoPlan.CardPlay>>;

namespace NecoBowl.Core.Sport.Tactics;

/// <summary>
/// Game state container that tracks cards placed by the players. This is consolidated into a <see cref="NecoPlan" /> once
/// the inputs are done being received. Note that in an online scenario, it is probable that inputs from the client's
/// opponent would only be received after the player has submitted their own. Therefore, the <see cref="CardPlays" />
/// dictionary likely only contains populated data for one player until <see cref="Finished" /> is true. <p /> Turns are
/// applied to a <see cref="NecoPush" />.
/// </summary>
internal class NecoTurn
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly List<NecoInput> AcceptedInputs = new();

    private readonly CardPlayMap CardPlays = new();
    public readonly NecoPlayerPair PlayerPair;

    public readonly uint TurnIndex;

    public NecoTurn(uint turnIndex, NecoPlayerPair playerPair)
    {
        TurnIndex = turnIndex;
        PlayerPair = playerPair;

        // Prepare dicts
        foreach (var player in playerPair.Enumerate()) {
            CardPlays[player.Id] = new();
        }
    }

    private NecoTurn(uint turnIndex, NecoPlayerPair playerPair, CardPlayMap plays)
        : this(turnIndex, playerPair)
    {
        CardPlays = new(plays);
    }

    public bool Finished { get; private set; }
    public uint BaseMoney => 10;

    public IEnumerable<NecoPlan.CardPlay> AllCardPlays
        => CardPlays.Values.Aggregate((orig, next) => orig.Concat(next).ToList());

    public IReadOnlyDictionary<NecoPlayerRole, List<NecoPlan.CardPlay>> CardPlaysByRole
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
    public NecoTurn NextTurn()
    {
        return new(TurnIndex + 1, PlayerPair, CardPlays);
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

        CardPlays[input.PlayerId].Add(new(input.PlayerId, input.Card, input.Position));
        return NecoInputResponse.Success();
    }

    private NecoInputResponse ProcessInput(NecoInput.SetPlanMod input)
    {
        if (!CardPlays.Values.Any(list => list.Any(cp => cp.Card == input.Card))) {
            throw new NecoInputException($"card {input.Card} not found in this turn");
        }

        input.Card.Options.SetValue(input.OptionIdentifier, input.OptionValue);
        return NecoInputResponse.Success();
    }
}