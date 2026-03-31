using System.Net.Http.Json;
using Virtual_Factory.Models;

namespace Virtual_Factory.Services
{
    public sealed class WorkOrderAdapter : IWorkOrderAdapter
    {
        private readonly HttpClient _http;

        public WorkOrderAdapter(HttpClient http) => _http = http;

        public async Task<IReadOnlyList<WorkOrder>> GetByEquipmentAsync(string equipmentId)
        {
            var encoded = Uri.EscapeDataString(equipmentId);
            return await _http.GetFromJsonAsync<List<WorkOrder>>(
                $"api/mock/work-orders?equipmentName={encoded}") ?? [];
        }
    }
}
