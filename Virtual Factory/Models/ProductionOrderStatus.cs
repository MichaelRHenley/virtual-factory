namespace Virtual_Factory.Models
{
    /// <summary>
    /// Represents the lifecycle status of a <see cref="ProductionOrder"/>.
    /// Values are assigned explicit integers so serialised representations
    /// remain stable if members are reordered in future.
    /// </summary>
    public enum ProductionOrderStatus
    {
        /// <summary>The order has been created but not yet released for execution.</summary>
        Planned = 1,

        /// <summary>The order has been approved and released to the shop floor.</summary>
        Released = 2,

        /// <summary>Production is actively in progress for this order.</summary>
        Running = 3,

        /// <summary>All planned quantity has been produced and the order is closed.</summary>
        Completed = 4,

        /// <summary>The order has been cancelled before or during execution.</summary>
        Cancelled = 5,
    }
}
