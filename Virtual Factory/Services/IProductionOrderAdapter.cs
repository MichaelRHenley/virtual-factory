using Virtual_Factory.Dtos;

namespace Virtual_Factory.Services
{
    public interface IProductionOrderAdapter
    {
        Task<ProductionOrderDto?> GetActiveOrderAsync(string equipmentId);
        Task<List<ProductionOrderDto>> GetScheduledOrdersAsync(string equipmentId);
        Task<List<ProductionOrderDto>> GetAllAsync();
    }
}
