using Virtual_Factory.Endpoints;
using Virtual_Factory.Extensions;
using Virtual_Factory.Services;
using Microsoft.EntityFrameworkCore;
using Virtual_Factory.Data;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddVirtualFactoryServices();
builder.Services.AddScoped<TelemetryHistoryWriter>();
builder.Services.AddScoped<EquipmentStateEventWriter>();
builder.Services.AddHostedService<EquipmentStateEventService>();
builder.Services.AddHostedService<TelemetryHistorySnapshotService>();
builder.Services.AddScoped<IEquipmentAvailabilityService, EquipmentAvailabilityService>();
builder.Services.AddScoped<IEquipmentEventSummaryService, EquipmentEventSummaryService>();
var mockApiBaseUrl = builder.Configuration["MockApi:BaseUrl"] ?? "http://localhost:5177";
void ConfigureMockClient(HttpClient c) =>
    c.BaseAddress = new Uri(mockApiBaseUrl.TrimEnd('/') + "/");

builder.Services.AddHttpClient<IWorkOrderAdapter, WorkOrderAdapter>(ConfigureMockClient);
builder.Services.AddHttpClient<IScheduleAdapter, ScheduleAdapter>(ConfigureMockClient);
builder.Services.AddHttpClient<IMaterialAdapter, MaterialAdapter>(ConfigureMockClient);
builder.Services.AddScoped<IOperationalContextService, OperationalContextService>();
builder.Services.AddSingleton<IAssetHierarchyService, AssetHierarchyService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapAssetEndpoints();
app.MapTelemetryEndpoints();
app.MapEquipmentEndpoints();
app.MapMockOperationalEndpoints();

await app.Services.GetRequiredService<ISeedLoader>().LoadAsync();

app.Run();