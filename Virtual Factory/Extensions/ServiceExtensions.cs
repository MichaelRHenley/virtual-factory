using Virtual_Factory.Repositories;
using Virtual_Factory.Services;

namespace Virtual_Factory.Extensions
{
    /// <summary>Extension methods for registering Virtual Factory services and repositories.</summary>
    public static class ServiceExtensions
    {
        /// <summary>
        /// Registers all in-memory repositories and application services with the DI container.
        /// </summary>
        public static IServiceCollection AddVirtualFactoryServices(this IServiceCollection services)
        {
            services.AddSingleton<IAssetRepository, InMemoryAssetRepository>();
            services.AddSingleton<ITelemetryPointRepository, InMemoryTelemetryPointRepository>();
            services.AddSingleton<IWorkOrderRepository, InMemoryWorkOrderRepository>();
            services.AddSingleton<IScheduleRepository, InMemoryScheduleRepository>();
            services.AddSingleton<IEventRepository, InMemoryEventRepository>();
            services.AddSingleton<IMaterialRepository, InMemoryMaterialRepository>();

            services.AddSingleton<ILatestPointValueStore, InMemoryLatestPointValueStore>();

            services.AddSingleton<ISeedLoader, JsonSeedLoader>();

            services.AddHostedService<TelemetrySimulationService>();

            services.AddHostedService<MqttNamespaceService>();
            return services;
        }
    }
}
