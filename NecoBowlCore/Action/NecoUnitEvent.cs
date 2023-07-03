namespace neco_soft.NecoBowlCore.Action;

public static class NecoUnitEvent
{
    public class UnitPushedOtherEventArgs : EventArgs
    {
        
    }
}

public class NecoUnitEventHandler
{
    public delegate void UnitPushedOtherHandler(NecoUnit pusher, NecoUnit receiver);
    public event UnitPushedOtherHandler? UnitPushedOther;

    public void OnUnitPushedOther(NecoUnit pusher, NecoUnit receiver)
    {
        UnitPushedOther?.Invoke(pusher, receiver);
    }
}