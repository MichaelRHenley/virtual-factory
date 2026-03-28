using Virtual_Factory.Models;

namespace Virtual_Factory.Repositories
{
    /// <summary>Read/write store for <see cref="TelemetryPointDefinition"/> records.</summary>
    public interface ITelemetryPointRepository
    {
        /// <summary>Adds or replaces a telemetry point definition in the store.</summary>
        void Add(TelemetryPointDefinition point);

        /// <summary>Returns all telemetry point definitions in the store.</summary>
        IReadOnlyList<TelemetryPointDefinition> GetAll();

        /// <summary>Returns all telemetry points belonging to the given <paramref name="assetId"/>.</summary>
        IReadOnlyList<TelemetryPointDefinition> GetByAssetId(string assetId);

        /// <summary>Returns the telemetry point with the given MQTT <paramref name="topic"/>, or <c>null</c> if not found.</summary>
        TelemetryPointDefinition? GetByTopic(string topic);
    }
}
