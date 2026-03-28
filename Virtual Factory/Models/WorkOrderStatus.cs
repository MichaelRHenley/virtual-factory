namespace Virtual_Factory.Models
{
    /// <summary>
    /// Represents the lifecycle status of a <see cref="WorkOrder"/>.
    /// Values are assigned explicit integers so serialised representations
    /// remain stable if members are reordered in future.
    /// </summary>
    public enum WorkOrderStatus
    {
        /// <summary>The work order has been created but not yet actioned.</summary>
        New = 1,

        /// <summary>The work order is open and awaiting assignment or scheduling.</summary>
        Open = 2,

        /// <summary>Work is actively being carried out on this order.</summary>
        InProgress = 3,

        /// <summary>Work has been temporarily paused, pending parts, approval, or resources.</summary>
        OnHold = 4,

        /// <summary>All work has been performed and the order is closed.</summary>
        Completed = 5,

        /// <summary>The work order has been cancelled before or during execution.</summary>
        Cancelled = 6,
    }
}
