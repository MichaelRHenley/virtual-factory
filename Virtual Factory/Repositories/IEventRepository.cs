using Virtual_Factory.Models;

namespace Virtual_Factory.Repositories
{
    /// <summary>Append-only, ordered store for <see cref="RecentEvent"/> records.</summary>
    public interface IEventRepository
    {
        /// <summary>Appends an event to the store.</summary>
        void Add(RecentEvent evt);

        /// <summary>Returns the most recent <paramref name="count"/> events across all assets.</summary>
        IReadOnlyList<RecentEvent> GetRecent(int count = 100);

        /// <summary>Returns the most recent <paramref name="count"/> events for a specific asset.</summary>
        IReadOnlyList<RecentEvent> GetByAsset(string assetId, int count = 50);
    }
}
