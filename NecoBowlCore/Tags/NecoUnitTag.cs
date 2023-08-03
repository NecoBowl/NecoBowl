namespace neco_soft.NecoBowlCore.Tags;

public enum NecoUnitTag
{
    TheBall,
    
    /// <summary>
    /// Can be picked up by a unit that moves onto its space.
    /// </summary>
    Item,
    
    /// <summary>
    /// Takes no damage unless the damage is greater than the unit's current health.
    /// </summary>
    Defender,
    
    /// <summary>
    /// (Preempt) Upon colliding with a unit, pushes that unit in the direction this unit was moving.
    /// That unit's action is not consumed this step, although it does not get to act.
    /// </summary>
    Pusher,
    
    /// <summary>
    /// Like <see cref="Pusher"/>, but the push receiver does not get to execute its action afterward.
    /// </summary>
    Shover, 
    
    /// <summary>
    /// Intercepts passes on a vector that touches this unit's square.
    /// </summary>
    Interceptor,
    
    /// <summary>
    /// Takes no damage when attacking from the flank or rear.
    /// </summary>
    Assassin,

    /// <summary>
    /// Can attack the spaces to the left and right of the target space.
    /// </summary>
    Opportunist,
    
    /// <summary>
    /// Units cannot be placed next to this one.
    /// </summary>
    Smelly,
    
    Test,
}