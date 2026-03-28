# Roadmap

## Current State – v0.1

The project provides:

- ISA-95 asset hierarchy domain model with full seed data (enterprise → equipment)
- Domain models: `Asset`, `WorkOrder`, `MaintenanceRequest`, `ProductionOrder`, `ScheduleEntry`, `MetadataProfile`, `TelemetryPointDefinition`, `LatestPointValue`, `RecentEvent`
- Enums: `AssetType`, `WorkOrderStatus`, `ProductionOrderStatus`
- JSON metadata profiles per asset type (`/Profiles/`)
- JSON seed fixtures for assets, work orders, events, schedules, and materials (`/SeedData/`)
- MQTT topic naming conventions (`/Docs/mqtt-topics.md`)
- Placeholder layers: services, repositories, endpoints, extensions, infrastructure

## Near-Term – v0.2

- [ ] In-memory repository implementations for `Asset`, `WorkOrder`, `ScheduleEntry`, `ProductionOrder`
- [ ] Seed loader service that reads `/SeedData/*.json` and populates repositories on startup
- [ ] REST API: `GET /api/assets`, `GET /api/assets/{id}`, `GET /api/assets/{id}/children`
- [ ] REST API: `GET /api/workorders`, `POST /api/workorders`, `PATCH /api/workorders/{id}/status`
- [ ] REST API: `GET /api/schedules`, `GET /api/events/recent`
- [ ] Profile loader service that reads `/Profiles/*.json` and resolves point definitions
- [ ] Swagger / OpenAPI documentation

## Mid-Term – v0.3

- [ ] MQTT subscriber integration (Mosquitto or HiveMQ CE via `start-mqtt.ps1`)
- [ ] Simulator service that publishes synthetic telemetry on a configurable interval
- [ ] `GET /api/telemetry/latest` — return latest cached point values
- [ ] `POST /api/write` — accept manual point writes and publish to MQTT
- [ ] Live event store backed by `RecentEvent` model with configurable ring-buffer size

## Future – v0.4+

- [ ] Server-Sent Events (SSE) or WebSocket feed for live telemetry streaming
- [ ] Production order tracking with basic OEE-style metrics (availability, performance, quality)
- [ ] Maintenance request lifecycle: Open → InReview → Approved → InProgress → Resolved
- [ ] i3X-style semantic mapping annotations in profile definitions
- [ ] Export to Sparkplug B envelope format for UNS interoperability testing
- [ ] `seed-factory.ps1` script for resetting and reseeding the factory without a full restart

## Out of Scope

The following items are intentionally excluded from this project:

- Database persistence (EF Core, SQL, Cosmos DB, or similar)
- Authentication and authorisation
- Multi-tenancy
- Cloud deployment or container orchestration configuration
- Vendor-specific PLC, SCADA, or DCS protocol adapters
