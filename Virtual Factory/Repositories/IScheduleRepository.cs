using Virtual_Factory.Models;

namespace Virtual_Factory.Repositories
{
    /// <summary>Read/write store for <see cref="ScheduleEntry"/> records.</summary>
    public interface IScheduleRepository
    {
        /// <summary>Adds or replaces a schedule entry in the store.</summary>
        void Add(ScheduleEntry entry);

        /// <summary>Returns all schedule entries in the store.</summary>
        IReadOnlyList<ScheduleEntry> GetAll();

        /// <summary>Returns the schedule entry with the given id, or <c>null</c> if not found.</summary>
        ScheduleEntry? GetById(string id);

        /// <summary>Returns all schedule entries scoped to the given asset id.</summary>
        IReadOnlyList<ScheduleEntry> GetByAsset(string assetId);
    }
}
