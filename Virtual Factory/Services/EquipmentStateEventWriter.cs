using Virtual_Factory.Data;
using Virtual_Factory.Models;

namespace Virtual_Factory.Services
{
    public class EquipmentStateEventWriter
    {
        private readonly AppDbContext _db;

        public EquipmentStateEventWriter(AppDbContext db)
        {
            _db = db;
        }

        public async Task WriteEventsAsync(
            EquipmentStateSnapshot current,
            EquipmentStateSnapshot? previous,
            CancellationToken cancellationToken = default)
        {
            var events = new List<EquipmentStateEvent>();

            if (previous == null)
            {
                events.Add(new EquipmentStateEvent
                {
                    EquipmentName = current.EquipmentName,
                    EventType = "equipment-seen",
                    PreviousState = null,
                    NewState = "online",
                    TimestampUtc = current.TimestampUtc,
                    Source = current.Source
                });

                // Record initial run and alarm states so availability calculations and recent
                // event panels populate immediately after a reset, without waiting for the
                // first transition to occur.
                if (!string.Equals(current.RunState, "unknown", StringComparison.OrdinalIgnoreCase))
                {
                    events.Add(new EquipmentStateEvent
                    {
                        EquipmentName = current.EquipmentName,
                        EventType = "run-state-changed",
                        PreviousState = null,
                        NewState = current.RunState,
                        TimestampUtc = current.TimestampUtc,
                        Source = current.Source
                    });
                }

                events.Add(new EquipmentStateEvent
                {
                    EquipmentName = current.EquipmentName,
                    EventType = "alarm-state-changed",
                    PreviousState = null,
                    NewState = current.AlarmState,
                    TimestampUtc = current.TimestampUtc,
                    Source = current.Source
                });
            }
            else
            {
                if (!string.Equals(previous.RunState, current.RunState, StringComparison.OrdinalIgnoreCase))
                {
                    events.Add(new EquipmentStateEvent
                    {
                        EquipmentName = current.EquipmentName,
                        EventType = "run-state-changed",
                        PreviousState = previous.RunState,
                        NewState = current.RunState,
                        TimestampUtc = current.TimestampUtc,
                        Source = current.Source
                    });
                }

                if (!string.Equals(previous.AlarmState, current.AlarmState, StringComparison.OrdinalIgnoreCase))
                {
                    events.Add(new EquipmentStateEvent
                    {
                        EquipmentName = current.EquipmentName,
                        EventType = "alarm-state-changed",
                        PreviousState = previous.AlarmState,
                        NewState = current.AlarmState,
                        TimestampUtc = current.TimestampUtc,
                        Source = current.Source
                    });
                }

                if (!string.Equals(previous.ConnectivityState, current.ConnectivityState, StringComparison.OrdinalIgnoreCase))
                {
                    events.Add(new EquipmentStateEvent
                    {
                        EquipmentName = current.EquipmentName,
                        EventType = "connectivity-state-changed",
                        PreviousState = previous.ConnectivityState,
                        NewState = current.ConnectivityState,
                        TimestampUtc = current.TimestampUtc,
                        Source = current.Source
                    });
                }
            }

            if (events.Count == 0)
                return;

            await _db.EquipmentStateEvents.AddRangeAsync(events, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}