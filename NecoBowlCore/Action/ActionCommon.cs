namespace neco_soft.NecoBowlCore.Action;

public class NecoUnitActionException : ApplicationException
{
    public NecoUnitActionException()
    { }

    public NecoUnitActionException(string message) : base(message)
    { }

    public NecoUnitActionException(string message, Exception inner) : base(message, inner)
    { }
}