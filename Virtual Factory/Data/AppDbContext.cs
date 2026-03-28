using Microsoft.EntityFrameworkCore;
using Virtual_Factory.Models;

namespace Virtual_Factory.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<TelemetryPointHistory> TelemetryPointHistories
            => Set<TelemetryPointHistory>();

        public DbSet<EquipmentStateEvent> EquipmentStateEvents
    => Set<EquipmentStateEvent>();
    }
}