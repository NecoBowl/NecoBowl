using neco_soft.NecoBowlCore.Action;
using neco_soft.NecoBowlCore.Tactics;
using neco_soft.NecoBowlCore.Tags;

namespace neco_soft.NecoBowlCore.Input;

public interface INecoInputReceiver<in T>
    where T : NecoInput
{
    public void ProcessInput(T input);
}

public abstract class NecoInput
{
    public NecoPlayerId PlayerId;

    public NecoInput(NecoPlayer player)
    {
        PlayerId = player.Id;
    }
    
    public class PlaceCard : NecoInput
    {
        public readonly NecoCard Card;
        public readonly Vector2i Position;

        public PlaceCard(NecoPlayer player, NecoCard card, Vector2i position)
            : base(player)
        {
            Card = card;
            Position = position;
        }
    }
    
    public class SetPlanMod : NecoInput
    {
        public readonly NecoCard Card;
        public readonly NecoCardOptionValue Mod;

        public SetPlanMod(NecoPlayer player, NecoCard card, NecoCardOptionValue mod)
            : base(player)
        {
            Card = card;
            Mod = mod;
        }
    }
}