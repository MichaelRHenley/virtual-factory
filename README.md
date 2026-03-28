# Virtual Factory

A simulated manufacturing telemetry platform that demonstrates real-time equipment monitoring, alarm lifecycle tracking, and event-duration analytics using an industrial-style architecture.

This project models how modern Factory OS dashboards evolve from raw telemetry streams into operational intelligence systems supporting downtime tracking, alarm escalation, and maintenance KPIs.

---

## Overview

Virtual Factory simulates a production environment where machine telemetry is ingested, interpreted into state transitions, and surfaced through a live operations dashboard.

Instead of visualizing raw tag values alone, the system derives:

* alarm activity windows
* stop events
* equipment runtime state
* 24-hour alarm frequency
* longest active alarms
* machine summary status indicators

This mirrors how real Ignition, MES, and Unified Namespace dashboards operate in production environments.

---

## Current Features

### Live Telemetry Dashboard

Displays latest values per equipment:

* run status
* alarm state
* temperatures
* counters
* vibration metrics
* speeds

---

### Equipment Summary Tiles

Automatically calculates:

* total equipment count
* running machines
* stopped machines
* machines with active alarms
* machines in warm condition

---

### Alarm Lifecycle Tracking

Detects transitions between:

```
normal → alarm
alarm → cleared
```

Tracks:

* active alarm duration
* longest current alarm
* alarm state per machine

---

### 24-Hour Event Metrics

Per-equipment rolling window analytics:

* alarm count (24h)
* stop count (24h)
* latest event detected
* longest active alarm duration

---

### Event Summary API

Structured endpoint:

```
/api/equipment/event-summary
```

Returns:

* latest event
* active alarm duration
* stop counts
* alarm counts
* equipment status snapshot

Designed for dashboard consumption or external integration.

---

### Offline Detection

Endpoint:

```
/api/equipment/offline-status
```

Determines whether telemetry has stopped updating for a machine.

Supports:

* stale device detection
* communications monitoring
* edge connectivity diagnostics

---

## Architecture

Telemetry pipeline:

```
Simulator / MQTT-style source
        ↓
Latest Value Cache
        ↓
Telemetry History Writer
        ↓
State Transition Detection
        ↓
EquipmentStateEvents table
        ↓
Event Summary Service
        ↓
Dashboard UI
```

This architecture mirrors ISA-95 style equipment monitoring layers used in MES platforms.

---

## Event Model

Machine behavior is interpreted from state transitions instead of raw tag values.

Examples:

| Transition        | Meaning            |
| ----------------- | ------------------ |
| normal → alarm    | alarm started      |
| alarm → normal    | alarm cleared      |
| running → stopped | downtime started   |
| stopped → running | production resumed |

These transitions generate structured records in:

```
EquipmentStateEvents
```

Which enables:

* MTTR analytics
* downtime tracking
* alarm frequency ranking
* escalation scoring

---

## Example Event Summary Output

```
{
  "equipmentId": "PRESS-BRAKE-01",
  "stopCount24h": 0,
  "alarmCount24h": 3,
  "latestEvent": {
    "eventType": "Alarm",
    "eventName": "High Temp",
    "state": "Active",
    "durationSeconds": 720
  },
  "longestCurrentAlarm": {
    "eventName": "High Temp",
    "durationSeconds": 2520
  }
}
```

---

## Technology Stack

Backend:

* ASP.NET Core
* Entity Framework Core
* SQL Server

Frontend:

* JavaScript
* HTML
* CSS

Architecture patterns:

* event-driven telemetry interpretation
* equipment state transitions
* rolling-window analytics
* summary aggregation services

---

## Roadmap

Planned enhancements:

### Event Timeline API

```
/api/equipment/{equipmentId}/event-timeline
```

Machine-level history visualization:

* alarm start/clear
* stop/resume
* duration tracking

---

### Downtime Analytics

Derived metrics:

* MTTR
* MTBF
* alarm frequency ranking
* stop duration trends

---

### Severity Scoring Engine

Equipment prioritization based on:

* active alarm presence
* alarm duration
* stop frequency
* recent alarm density

---

### Predictive Escalation Signals

Future support for:

* threshold drift detection
* abnormal vibration patterns
* temperature anomaly detection

---

## Project Purpose

Virtual Factory is part of a broader Manufacturing Core Suite initiative exploring how modern industrial systems combine:

* telemetry streams
* structured equipment events
* real-time dashboards
* maintenance intelligence layers

into a unified Factory OS architecture.

The goal is to demonstrate how operational telemetry evolves into actionable manufacturing insight.
