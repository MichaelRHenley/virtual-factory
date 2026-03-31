using System.Net.Http.Json;
using Virtual_Factory.Models;

namespace Virtual_Factory.Services
{
    public sealed class MaterialAdapter : IMaterialAdapter
    {
        private readonly HttpClient _http;

        public MaterialAdapter(HttpClient http) => _http = http;

        public async Task<IReadOnlyList<Material>> GetByEquipmentAsync(string equipmentId)
        {
            var encoded = Uri.EscapeDataString(equipmentId);
            return await _http.GetFromJsonAsync<List<Material>>(
                $"api/mock/materials?equipmentName={encoded}") ?? [];
        }
    }
}
