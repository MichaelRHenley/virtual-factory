using System.Collections.Generic;
using System.Threading.Tasks;
using Virtual_Factory.Dtos;

namespace Virtual_Factory.Services
{
    public interface IMaintenanceAdapter
    {
        Task<List<PreventiveMaintenanceTaskDto>> GetOpenPmTasksAsync(string equipmentId);
        Task<List<PreventiveMaintenanceTaskDto>> GetOverduePmTasksAsync(string equipmentId);
        Task<List<PreventiveMaintenanceTaskDto>> GetUpcomingPmTasksAsync(string equipmentId);
    }
}
