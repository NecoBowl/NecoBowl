namespace neco_soft.NecoBowlCore.Tags;

public enum NecoUnitTag
{
    TheBall,

    /// <summary>
    ///     Can be picked up by a unit that moves onto its space.
    /// </summary>
    Item,

    /// <summary>
    ///     When this unit moves onto or contests the square of a unit tagged with Item, this unit puts the Item into its
    ///     inventory.
    /// </summary>
    Carrier,

    /// <summary>
    ///     Cannot attack units by moving onto their space.
    /// </summary>
    Defender,

    Bossy,

    Butterfingers,

    /// <summary>
    ///     Attacks any unit that attacks it.
    /// </summary>
    Counterattack,

    /// <summary>
    ///     Resets health to its maximum after each step.
    /// </summary>
    Regenerator,

    /// <summary>
    ///     (Preempt) Upon colliding with a unit, pushes that unit in the direction this unit was moving.
    ///     That unit's action is not consumed this step, although it does not get to act.
    /// </summary>
    Pusher,

    /// <summary>
    ///     Like <see cref="Pusher" />, but the push receiver does not get to execute its action afterward.
    /// </summary>
    UNIMPL_Shover,

    /// <summary>
    ///     Intercepts passes on a vector that touches this unit's square.
    /// </summary>
    UNIMPL_Interceptor,

    /// <summary>
    ///     Takes no damage when attacking from the flank or rear.
    /// </summary>
    UNIMPL_Assassin,

    /// <summary>
    ///     Can attack the spaces to the left and right of the target space.
    /// </summary>
    UNIMPL_Opportunist,

    /// <summary>
    ///     Units cannot be placed next to this unit. Cannot place this unit next to others.
    /// </summary>
    UNIMPL_Smelly,

    Test
}
