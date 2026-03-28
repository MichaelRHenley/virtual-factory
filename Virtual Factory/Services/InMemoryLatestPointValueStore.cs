using System.Collections.Concurrent;
using Virtual_Factory.Models;

namespace Virtual_Factory.Services
{
    /// <summary>
    /// Thread-safe, in-memory implementation of <see cref="ILatestPointValueStore"/>.
    /// Values are keyed by MQTT topic and updated on every incoming write.
    /// </summary>
    public sealed class InMemoryLatestPointValueStore : ILatestPointValueStore
    {
        private readonly ConcurrentDictionary<string, LatestPointValue> _store = new(StringComparer.Ordinal);

        /// <inheritdoc/>
        public IEnumerable<LatestPointValue> GetAll() => _store.Values;

        /// <inheritdoc/>
        public LatestPointValue? GetByTopic(string topic) =>
            _store.TryGetValue(topic, out var value) ? value : null;

        /// <inheritdoc/>
        public void SetValue(LatestPointValue value) =>
            _store[value.Topic] = value;

        /// <inheritdoc/>
        public bool Exists(string topic) => _store.ContainsKey(topic);
    }
}
