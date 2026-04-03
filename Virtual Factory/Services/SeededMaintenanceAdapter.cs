using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Virtual_Factory.Dtos;

namespace Virtual_Factory.Services
{
    public sealed class SeededMaintenanceAdapter : IMaintenanceAdapter
    {
        private readonly List<PreventiveMaintenanceTaskDto> _tasks;

        public SeededMaintenanceAdapter()
        {
            var now = DateTime.UtcNow;

            _tasks = new List<PreventiveMaintenanceTaskDto>
            {
                new PreventiveMaintenanceTaskDto
                {
                    TaskId = "PM-DB-001",
                    EquipmentId = "DEBURRING-STATION-01",
                    TaskDescription = "Lubrication check on deburring spindle bearings",
                    TaskType = "Lubrication",
                    DueDateUtc = now.AddDays(-2),
                    DueRuntimeHours = 200,
                    Status = "Open",
                    Priority = "High",
                    IsOverdue = true,
                },
                new PreventiveMaintenanceTaskDto
                {
                    TaskId = "PM-DB-002",
                    EquipmentId = "DEBURRING-STATION-01",
                    TaskDescription = "Spindle inspection",
                    TaskType = "Inspection",
                    DueDateUtc = now.AddDays(3),
                    DueRuntimeHours = 220,
                    Status = "Open",
                    Priority = "Medium",
                    IsOverdue = false,
                },
                new PreventiveMaintenanceTaskDto
                {
                    TaskId = "PM-DB-003",
                    EquipmentId = "DEBURRING-STATION-01",
                    TaskDescription = "Replace coolant filter",
                    TaskType = "Replacement",
                    DueDateUtc = now.AddDays(-10),
                    DueRuntimeHours = 180,
                    Status = "Completed",
                    Priority = "Medium",
                    IsOverdue = false,
                },

                new PreventiveMaintenanceTaskDto
                {
                    TaskId = "PM-IN-001",
                    EquipmentId = "INSPECTION-STATION-01",
                    TaskDescription = "Camera lens cleaning",
                    TaskType = "Cleaning",
                    DueDateUtc = now.AddDays(1),
                    DueRuntimeHours = null,
                    Status = "Open",
                    Priority = "Low",
                    IsOverdue = false,
                },
                new PreventiveMaintenanceTaskDto
                {
                    TaskId = "PM-IN-002",
                    EquipmentId = "INSPECTION-STATION-01",
                    TaskDescription = "Fixture alignment check",
                    TaskType = "Inspection",
                    DueDateUtc = now.AddDays(-1),
                    DueRuntimeHours = null,
                    Status = "Open",
                    Priority = "High",
                    IsOverdue = true,
                },
                new PreventiveMaintenanceTaskDto
                {
                    TaskId = "PM-IN-003",
                    EquipmentId = "INSPECTION-STATION-01",
                    TaskDescription = "Archive inspection images",
                    TaskType = "Administrative",
                    DueDateUtc = now.AddDays(-5),
                    DueRuntimeHours = null,
                    Status = "Completed",
                    Priority = "Low",
                    IsOverdue = false,
                },

                new PreventiveMaintenanceTaskDto
                {
                    TaskId = "PM-CP-001",
                    EquipmentId = "CASE-PACKER-01",
                    TaskDescription = "Lubricate conveyor chains",
                    TaskType = "Lubrication",
                    DueDateUtc = now.AddDays(5),
                    DueRuntimeHours = 300,
                    Status = "Open",
                    Priority = "Medium",
                    IsOverdue = false,
                },
                new PreventiveMaintenanceTaskDto
                {
                    TaskId = "PM-CP-002",
                    EquipmentId = "CASE-PACKER-01",
                    TaskDescription = "Check case forming cylinders",
                    TaskType = "Inspection",
                    DueDateUtc = now.AddDays(-3),
                    DueRuntimeHours = 280,
                    Status = "Open",
                    Priority = "High",
                    IsOverdue = true,
                },
                new PreventiveMaintenanceTaskDto
                {
                    TaskId = "PM-CP-003",
                    EquipmentId = "CASE-PACKER-01",
                    TaskDescription = "Verify safety interlocks",
                    TaskType = "Inspection",
                    DueDateUtc = now.AddDays(-20),
                    DueRuntimeHours = 250,
                    Status = "Completed",
                    Priority = "High",
                    IsOverdue = false,
                },
            };
        }

        private static string Normalize(string equipmentId) =>
            (equipmentId ?? string.Empty).Trim().ToUpperInvariant();

        public Task<List<PreventiveMaintenanceTaskDto>> GetOpenPmTasksAsync(string equipmentId)
        {
            var key = Normalize(equipmentId);
            var list = _tasks
                .Where(t => Normalize(t.EquipmentId) == key &&
                            !string.Equals(t.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                .ToList();
            return Task.FromResult(list);
        }

        public Task<List<PreventiveMaintenanceTaskDto>> GetOverduePmTasksAsync(string equipmentId)
        {
            var key = Normalize(equipmentId);
            var list = _tasks
                .Where(t => Normalize(t.EquipmentId) == key &&
                            !string.Equals(t.Status, "Completed", StringComparison.OrdinalIgnoreCase) &&
                            t.IsOverdue)
                .ToList();
            return Task.FromResult(list);
        }

        public Task<List<PreventiveMaintenanceTaskDto>> GetUpcomingPmTasksAsync(string equipmentId)
        {
            var key = Normalize(equipmentId);
            var now = DateTime.UtcNow;
            var list = _tasks
                .Where(t => Normalize(t.EquipmentId) == key &&
                            !string.Equals(t.Status, "Completed", StringComparison.OrdinalIgnoreCase) &&
                            !t.IsOverdue &&
                            t.DueDateUtc > now)
                .OrderBy(t => t.DueDateUtc)
                .ToList();
            return Task.FromResult(list);
        }
    }
}
