using Microsoft.Extensions.Hosting;

namespace Virtual_Factory.Services
{
    public class TelemetryHistorySnapshotService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public TelemetryHistorySnapshotService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(3));

            while (!stoppingToken.IsCancellationRequested)
            {
                await timer.WaitForNextTickAsync(stoppingToken);

                using var scope = _scopeFactory.CreateScope();

                var store = scope.ServiceProvider.GetRequiredService<ILatestPointValueStore>();
                var writer = scope.ServiceProvider.GetRequiredService<TelemetryHistoryWriter>();

                var latest = store.GetAll();

                await writer.WriteAsync(latest);
            }
        }
    }
}