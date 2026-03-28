using Virtual_Factory.Models;

namespace Virtual_Factory.Repositories
{
    /// <summary>Read/write store for <see cref="WorkOrder"/> records.</summary>
    public interface IWorkOrderRepository
    {
        /// <summary>Adds or replaces a work order in the store.</summary>
        void Add(WorkOrder workOrder);

        /// <summary>Returns all work orders in the store.</summary>
        IReadOnlyList<WorkOrder> GetAll();

        /// <summary>Returns the work order with the given id, or <c>null</c> if not found.</summary>
        WorkOrder? GetById(string id);

        /// <summary>Returns all work orders associated with the given asset id.</summary>
        IReadOnlyList<WorkOrder> GetByAsset(string assetId);
    }
}
