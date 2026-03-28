# Virtual Factory – Architecture

## Overview

Virtual Factory is a standalone ASP.NET Core simulation of a manufacturing environment, modelled on the ISA-95 asset hierarchy. It is designed as a self-contained sandbox for exploring Unified Namespace (UNS) patterns, MQTT-based telemetry routing, and manufacturing data models—without coupling to any vendor platform or cloud service.

## Project Structure

```
VirtualFactory/
├── Controllers/        # Minimal Web API controllers
├── Docs/               # Architecture, topic conventions, and roadmap
├── Dtos/               # Request/response transfer objects
├── Endpoints/          # Minimal API endpoint registrations
├── Extensions/         # IServiceCollection and IApplicationBuilder helpers
├── Infrastructure/     # In-memory stores and lightweight services
├── Models/             # Domain models (Asset, WorkOrder, ScheduleEntry, …)
├── Mqtt/               # MQTT topic conventions and example payloads
├── Profiles/           # JSON metadata profile definitions per asset type
├── Repositories/       # Repository interfaces and in-memory implementations
├── Scripts/            # Helper scripts for local development
├── SeedData/           # JSON seed fixtures for factory hierarchy and work data
├── Services/           # Application-layer simulation services
└── Tests/              # Unit and integration tests
```

## Core Concepts

### Asset Hierarchy (ISA-95)

Assets are organised in a five-level ISA-95 hierarchy:

| Level | AssetType  | Example                  |
|-------|------------|--------------------------|
| 1     | Enterprise | `virtual-enterprise`     |
| 2     | Site       | `site-alpha`             |
| 3     | Area       | `area-assembly`          |
| 4     | Line       | `line-a`                 |
| 5     | Equipment  | `conveyor-01`            |

Each asset's `Path` property mirrors its MQTT topic prefix, keeping asset addressing and topic addressing in sync without extra mapping.

### Unified Namespace (UNS)

All telemetry, state, and operational data is addressed through a topic-structured namespace:

```
{enterprise}/{site}/{area}/{line}/{equipment}/{pointName}
```

Example: `virtual-enterprise/site-alpha/area-assembly/line-a/conveyor-01/speed`

### Metadata Profiles

Each asset type references a `MetadataProfile` that defines its expected telemetry points, supported properties, and relationships. Profiles are stored as JSON in `/Profiles/` and are loaded at startup.

### Seed Data

All factory fixtures—assets, work orders, schedules, events, and materials—are loaded from `/SeedData/*.json` on startup. The simulation is entirely in-memory; it resets cleanly on each restart.

## Key Design Decisions

- **In-memory only** — No persistence layer. Safe to experiment with without managing database state.
- **Vendor-neutral** — No platform-specific SDKs, proprietary protocols, or EF Core annotations in the domain layer.
- **UNS-aligned** — Topic paths derive directly from the asset hierarchy, maintaining a single source of truth for addressing.
- **Profile-driven** — Asset shapes and telemetry point definitions are externalised to JSON profiles, not hardcoded.
- **Seed-driven** — Factory scenarios are swapped by editing files in `/SeedData/`, not by changing code.
