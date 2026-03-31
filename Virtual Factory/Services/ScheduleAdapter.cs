using System.Net.Http.Json;
using Virtual_Factory.Models;

namespace Virtual_Factory.Services
{
    public sealed class ScheduleAdapter : IScheduleAdapter
    {
        private readonly HttpClient _http;

        public ScheduleAdapter(HttpClient http) => _http = http;

        public async Task<IReadOnlyList<ScheduleEntry>> GetByEquipmentAsync(string equipmentId)
        {
            var encoded = Uri.EscapeDataString(equipmentId);
            return await _http.GetFromJsonAsync<List<ScheduleEntry>>(
                $"api/mock/schedules?equipmentName={encoded}") ?? [];
        }
    }
}
