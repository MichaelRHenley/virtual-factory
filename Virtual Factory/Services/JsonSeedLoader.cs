using System.Text.Json;
using Virtual_Factory.Models;
using Virtual_Factory.Repositories;

namespace Virtual_Factory.Services
{
    /// <summary>
    /// Reads the JSON files in the <c>SeedData</c> folder and populates the in-memory
    /// repositories. Called once during application startup, before the HTTP pipeline
    /// begins serving requests.
    /// </summary>
    public sealed class JsonSeedLoader : ISeedLoader
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
        };

        private readonly IWebHostEnvironment _env;
        private readonly IAssetRepository _assets;
        private readonly IWorkOrderRepository _workOrders;
        private readonly IScheduleRepository _schedules;
        private readonly IEventRepository _events;
        private readonly IMaterialRepository _materials;
        private readonly ITelemetryPointRepository _telemetryPoints;
        private readonly ILogger<JsonSeedLoader> _logger;

        public JsonSeedLoader(
            IWebHostEnvironment env,
            IAssetRepository assets,
            IWorkOrderRepository workOrders,
            IScheduleRepository schedules,
            IEventRepository events,
            IMaterialRepository materials,
            ITelemetryPointRepository telemetryPoints,
            ILogger<JsonSeedLoader> logger)
        {
            _env = env;
            _assets = assets;
            _workOrders = workOrders;
            _schedules = schedules;
            _events = events;
            _materials = materials;
            _telemetryPoints = telemetryPoints;
            _logger = logger;
        }

        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            var seedPath = Path.Combine(_env.ContentRootPath, "SeedData");

            await LoadFileAsync<Asset>(seedPath, "assets.json", _assets.Add, cancellationToken);
            await LoadFileAsync<TelemetryPointDefinition>(seedPath, "telemetryPoints.json", _telemetryPoints.Add, cancellationToken);
            await LoadFileAsync<WorkOrder>(seedPath, "workorders.json", _workOrders.Add, cancellationToken);
            await LoadFileAsync<ScheduleEntry>(seedPath, "schedules.json", _schedules.Add, cancellationToken);
            await LoadFileAsync<RecentEvent>(seedPath, "events.json", _events.Add, cancellationToken);
            await LoadFileAsync<Material>(seedPath, "materials.json", _materials.Add, cancellationToken);
        }

        private async Task LoadFileAsync<T>(
            string folder,
            string fileName,
            Action<T> add,
            CancellationToken cancellationToken)
        {
            var path = Path.Combine(folder, fileName);

            if (!File.Exists(path))
            {
                _logger.LogWarning("Seed file not found, skipping: {Path}", path);
                return;
            }

            try
            {
                await using var stream = File.OpenRead(path);
                var items = await JsonSerializer.DeserializeAsync<List<T>>(
                    stream, JsonOptions, cancellationToken);

                if (items is null)
                {
                    _logger.LogWarning("Seed file deserialised to null, skipping: {File}", fileName);
                    return;
                }

                foreach (var item in items)
                    add(item);

                _logger.LogInformation(
                    "Seeded {Count} {Type} record(s) from {File}",
                    items.Count, typeof(T).Name, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load seed file: {File}", fileName);
            }
        }
    }
}
