using NecoBowl.Core.Machine;

namespace NecoBowl.Core.Sport.Play;

public class UnitPushes : BaseMutation
{
    public readonly AbsoluteDirection Direction;
    public readonly NecoUnitId Pusher;
    public readonly NecoUnitId Receiver;

    public UnitPushes(NecoUnitId pusher, NecoUnitId receiver, AbsoluteDirection direction)
        : base(pusher)
    {
        Pusher = pusher;
        Receiver = receiver;
        Direction = direction;
    }

    public override string Description
        => $"{Pusher} pushes {Receiver} to the {Direction}";

    internal override void EarlyMutate(Playfield field, IPlayfieldChangeReceiver substepContext)
    {
        var pusher = field.GetUnit(Pusher);
        var receiver = field.GetUnit(Receiver, out var receiverPos);
        if (field.IsInBounds(receiverPos + Direction.ToVector2i())) {
            substepContext.BufferMovement(new(receiverPos + Direction.ToVector2i(), receiverPos, receiver));
        }
    }
}
