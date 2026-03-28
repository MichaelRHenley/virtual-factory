using Virtual_Factory.Models;

namespace Virtual_Factory.Services
{
    /// <summary>In-memory cache for the latest known value of each MQTT topic.</summary>
    public interface ILatestPointValueStore
    {
        /// <summary>Returns all cached values.</summary>
        IEnumerable<LatestPointValue> GetAll();

        /// <summary>
        /// Returns the cached value for the given <paramref name="topic"/>,
        /// or <c>null</c> if no value has been stored yet.
        /// </summary>
        LatestPointValue? GetByTopic(string topic);

        /// <summary>Inserts or replaces the cached value for the topic carried by <paramref name="value"/>.</summary>
        void SetValue(LatestPointValue value);

        /// <summary>Returns <c>true</c> if a value has been stored for <paramref name="topic"/>.</summary>
        bool Exists(string topic);
    }
}
