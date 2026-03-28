# Virtual Factory

## What is Virtual Factory?

Virtual Factory is a simulation sandbox for a manufacturing environment, built on ASP.NET Core. It models a realistic ISA-95 asset hierarchy and exposes telemetry, work orders, schedules, and production orders through a REST API and an optional MQTT interface.

It is intended as a reference implementation and development tool for:

- Exploring Unified Namespace (UNS) topic patterns
- Prototyping manufacturing data models and API shapes
- Testing integrations without a physical or commercial PLC/SCADA system
- Demonstrating ISA-95 hierarchy navigation in code

## Factory Layout

The default seed data models the following hierarchy:

```
Virtual Enterprise
└── Site Alpha
    ├── Assembly Area
    │   ├── Assembly Line A
    │   │   ├── Conveyor 01
    │   │   ├── Robot Arm 01
    │   │   └── Inspection Station 01
    │   └── Assembly Line B
    │       ├── Conveyor 02
    │       ├── Welding Cell 01
    │       └── Conveyor 03
    └── Packaging Area
        └── Packaging Line 1
            ├── Labeller 01
            ├── Case Packer 01
            └── Palletiser 01
```

## Domain Coverage

| Domain            | Status | Notes                                         |
|-------------------|--------|-----------------------------------------------|
| Asset Hierarchy   | ✅     | Full ISA-95 tree, seed-loaded from JSON        |
| Telemetry Points  | ✅     | Defined per equipment via metadata profiles    |
| Work Orders       | ✅     | CRUD-ready model, seed data provided           |
| Maintenance Reqs  | ✅     | Linked to equipment assets                     |
| Production Orders | ✅     | Line-level, with status lifecycle              |
| Schedule Entries  | ✅     | Shift, maintenance, and production windows     |
| MQTT Publish      | 🔜     | Planned — topic structure defined              |
| Persistence       | ❌     | Out of scope — in-memory only                  |
| Authentication    | ❌     | Out of scope for simulation sandbox            |

## Getting Started

1. Clone the repository.
2. Run `dotnet run` from the `Virtual Factory` project directory.
3. Navigate to `https://localhost:{port}/swagger` to explore the API.
4. Seed data is loaded automatically on startup from `/SeedData/*.json`.
5. Use the scripts in `/Scripts/` to start a local MQTT broker or re-seed the factory.

## Configuration

The simulation does not require any external dependencies to run. An optional MQTT broker connection can be configured in `appsettings.json` when MQTT features are enabled. See `/Docs/mqtt-topics.md` for topic conventions.
