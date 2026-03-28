# Seed Data

## Overview

The `/SeedData` directory contains JSON fixtures that are loaded at application startup to populate the in-memory stores. Seed data is intentionally minimal and vendor-neutral, modelling a realistic but fictional factory layout.

## Files

| File               | Schema            | Purpose                                                |
|--------------------|-------------------|--------------------------------------------------------|
| `assets.json`      | `Asset[]`         | Full ISA-95 hierarchy: enterprise → equipment          |
| `workorders.json`  | `WorkOrder[]`     | Sample work orders linked to equipment assets          |
| `events.json`      | `RecentEvent[]`   | Sample operational and telemetry events                |
| `schedules.json`   | `ScheduleEntry[]` | Shift, production, and maintenance schedule windows    |
| `materials.json`   | Material list     | Reference material and part codes used in production   |

## Asset Hierarchy

Seed assets cover the full ISA-95 hierarchy from Enterprise down to individual Equipment:

```
ent-01  Virtual Enterprise  (Enterprise)
└── site-01  Site Alpha  (Site)
    ├── area-01  Assembly Area  (Area)
    │   ├── line-01  Assembly Line A  (Line)
    │   │   ├── eq-01  conveyor-01
    │   │   ├── eq-02  robot-arm-01
    │   │   └── eq-03  inspection-station-01
    │   └── line-02  Assembly Line B  (Line)
    │       ├── eq-04  conveyor-02
    │       ├── eq-05  welding-cell-01
    │       └── eq-06  conveyor-03
    └── area-02  Packaging Area  (Area)
        └── line-03  Packaging Line 1  (Line)
            ├── eq-07  labeller-01
            ├── eq-08  case-packer-01
            └── eq-09  palletiser-01
```

## AssetType Values

The `assetType` field is an integer matching the `AssetType` enum:

| Value | AssetType  |
|-------|------------|
| 1     | Enterprise |
| 2     | Site       |
| 3     | Area       |
| 4     | Line       |
| 5     | Equipment  |

## Adding or Modifying Seed Data

1. Edit the relevant JSON file in `/SeedData/`.
2. Ensure `id` values are unique within each file.
3. Verify that `parentId` values in `assets.json` reference a valid asset `id`.
4. Keep `childrenIds` in sync with corresponding `parentId` references.
5. All timestamps must be ISO 8601 UTC strings (e.g. `"2025-06-01T08:00:00Z"`).
6. Restart the application; seed data is loaded fresh on each startup.
