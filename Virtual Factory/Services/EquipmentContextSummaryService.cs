using System.Text;
using Virtual_Factory.Dtos;

namespace Virtual_Factory.Services
{
    public sealed class EquipmentContextSummaryService : IEquipmentContextSummaryService
    {
        private readonly IOperationalContextService _context;

        public EquipmentContextSummaryService(IOperationalContextService context)
        {
            _context = context;
        }

        public async Task<EquipmentContextSummaryDto?> GetSummaryAsync(
            string equipmentId,
            CancellationToken cancellationToken = default)
        {
            var ctx = await _context.GetContextAsync(equipmentId, cancellationToken);

            if (ctx is null)
                return null;

            var sb = new StringBuilder();

            // Status and alarm state
            sb.Append($"Equipment {equipmentId.ToUpperInvariant()} is currently {ctx.CurrentStatus}");
            sb.AppendLine($" with {ctx.AlarmState} alarm state.");

            // Availability (1 h)
            if (ctx.Availability1h.HasValue)
                sb.AppendLine($"Availability over the last hour is {ctx.Availability1h.Value:F1}%.");
            else
                sb.AppendLine("Availability data is unavailable.");

            // 24 h counts
            var stopWord  = ctx.StopCount24h  == 1 ? "stop"  : "stops";
            var alarmWord = ctx.AlarmCount24h == 1 ? "alarm" : "alarms";
            sb.AppendLine(
                $"There were {ctx.StopCount24h} {stopWord} and " +
                $"{ctx.AlarmCount24h} {alarmWord} in the past 24 hours.");

            // Latest event
            if (ctx.LatestEvent is not null)
                sb.AppendLine(
                    $"Latest event: {ctx.LatestEvent.EventName} to {ctx.LatestEvent.State}.");
            else
                sb.AppendLine("No recent events.");

            // Active work order
            if (ctx.ActiveWorkOrder is not null)
                sb.AppendLine(
                    $"Active work order: {ctx.ActiveWorkOrder.WorkOrderNumber} — " +
                    $"{ctx.ActiveWorkOrder.Title} ({ctx.ActiveWorkOrder.Status}).");
            else
                sb.AppendLine("No active work order.");

            // Scheduled product
            if (ctx.ScheduledProduct is not null)
                sb.AppendLine(
                    $"Scheduled: {ctx.ScheduledProduct.Title} ({ctx.ScheduledProduct.ScheduleType}).");
            else
                sb.AppendLine("No scheduled product.");

            // Material status
            if (ctx.MaterialStatus is not null)
                sb.AppendLine(
                    $"Material {ctx.MaterialStatus.MaterialCode} is {ctx.MaterialStatus.StockStatus}.");
            else
                sb.AppendLine("No material data.");

            return new EquipmentContextSummaryDto
            {
                EquipmentId = equipmentId,
                SummaryText = sb.ToString().TrimEnd(),
            };
        }
    }
}
