# MQTT Topic Conventions

## Topic Structure

All topics follow the Unified Namespace (UNS) pattern:

```
{enterprise}/{site}/{area}/{line}/{equipment}/{pointName}
```

Each segment maps directly to an asset's `Name` property in the ISA-95 hierarchy. The full path up to (but not including) the point name matches the asset's `Path` property, keeping asset addressing and topic addressing in sync.

## Topic Segments

| Segment    | Maps To            | Example                     |
|------------|--------------------|-----------------------------|
| enterprise | Enterprise `Name`  | `virtual-enterprise`        |
| site       | Site `Name`        | `site-alpha`                |
| area       | Area `Name`        | `area-assembly`             |
| line       | Line `Name`        | `line-a`                    |
| equipment  | Equipment `Name`   | `conveyor-01`               |
| pointName  | Point `PointName`  | `speed`, `motor-temperature`|

## Example Topics

```
virtual-enterprise/site-alpha/area-assembly/line-a/conveyor-01/running-state
virtual-enterprise/site-alpha/area-assembly/line-a/conveyor-01/motor-temperature
virtual-enterprise/site-alpha/area-assembly/line-a/conveyor-01/speed
virtual-enterprise/site-alpha/area-assembly/line-a/conveyor-01/speed-setpoint
virtual-enterprise/site-alpha/area-assembly/line-a/conveyor-01/fault-code
virtual-enterprise/site-alpha/area-assembly/line-a/conveyor-01/cycle-count

virtual-enterprise/site-alpha/area-assembly/line-a/robot-arm-01/running-state
virtual-enterprise/site-alpha/area-assembly/line-a/robot-arm-01/fault-code
virtual-enterprise/site-alpha/area-assembly/line-a/robot-arm-01/speed

virtual-enterprise/site-alpha/area-assembly/line-a/inspection-station-01/running-state
virtual-enterprise/site-alpha/area-assembly/line-a/inspection-station-01/fault-code
virtual-enterprise/site-alpha/area-assembly/line-a/inspection-station-01/cycle-count

virtual-enterprise/site-alpha/area-assembly/line-b/welding-cell-01/running-state
virtual-enterprise/site-alpha/area-assembly/line-b/welding-cell-01/motor-temperature
virtual-enterprise/site-alpha/area-assembly/line-b/welding-cell-01/fault-code

virtual-enterprise/site-alpha/area-assembly/line-a/line-state
virtual-enterprise/site-alpha/area-assembly/line-a/throughput-rate
virtual-enterprise/site-alpha/area-assembly/line-a/shift-count
```

## Wildcard Subscriptions

| Pattern                                           | Matches                                        |
|---------------------------------------------------|------------------------------------------------|
| `virtual-enterprise/#`                            | All topics in the enterprise                   |
| `virtual-enterprise/site-alpha/#`                 | All topics for Site Alpha                      |
| `virtual-enterprise/+/area-assembly/#`            | All topics in the Assembly Area                |
| `virtual-enterprise/+/+/line-a/#`                 | All topics on Line A in any area               |
| `virtual-enterprise/+/+/+/conveyor-01/#`          | All points for every Conveyor 01               |
| `virtual-enterprise/+/+/+/+/motor-temperature`    | Motor temperature across all equipment         |
| `virtual-enterprise/+/+/+/+/fault-code`           | Fault codes across all equipment               |

## Quality of Service (QoS)

| Use Case              | Recommended QoS               |
|-----------------------|-------------------------------|
| Telemetry streaming   | QoS 0 (at most once)          |
| State changes         | QoS 1 (at least once)         |
| Setpoint writes       | QoS 1 (at least once)         |

## Retained Messages

State and latest-value topics should be published with `retain = true` so subscribers that connect mid-session receive the current state immediately without waiting for the next publish cycle.

## Payload Format

Payloads are UTF-8 encoded JSON objects:

```json
{
  "value": 47.3,
  "unit": "rpm",
  "quality": "Good",
  "timestamp": "2025-06-01T08:00:00Z",
  "source": "simulator"
}
```

Single scalar values (e.g. `"true"`, `"58.2"`) are also accepted for lightweight publish scenarios.
