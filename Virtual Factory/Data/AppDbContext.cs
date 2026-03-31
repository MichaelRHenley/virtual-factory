using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // SQL Server datetime2 columns have no timezone metadata, so EF Core
            // reads them back with Kind = Unspecified.  System.Text.Json then omits
            // the "Z" suffix, and browsers parse the string as local time instead of
            // UTC — causing timestamps to appear hours older than they are.
            // This converter stamps Kind = Utc on every DateTime read from the DB so
            // serialisation always emits the "Z" suffix.
            var utcConverter = new ValueConverter<DateTime, DateTime>(
                write => write,
                read  => DateTime.SpecifyKind(read, DateTimeKind.Utc));

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties()
                    .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?)))
                {
                    property.SetValueConverter(utcConverter);
                }
            }
        }
    }
}