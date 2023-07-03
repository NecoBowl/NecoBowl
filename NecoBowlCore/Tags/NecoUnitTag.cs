namespace neco_soft.NecoBowlCore.Tags;

public enum NecoUnitTag
{
    /// <summary>
    /// Can be picked up by a unit that moves onto its space.
    /// </summary>
    Item,
    
    /// <summary>
    /// Wins power ties in combat.
    /// </summary>
    Defender,
    
    /// <summary>
    /// (Preempt) Upon colliding with a unit, pushes that unit in the direction this unit was moving.
    /// That unit's action is not consumed this turn.
    /// </summary>
    Pusher,
    
    /// <summary>
    /// Can attack the spaces to the left and right of the target space.
    /// </summary>
    Opportunist,
    
    Test, 
}