# MQTT Example Topics

Sample topics for the Virtual Factory Unified Namespace. All topics follow the pattern:

```
{enterprise}/{site}/{area}/{line}/{equipment}/{pointName}
```

---

## Assembly Area – Line A

### Conveyor 01

```
virtual-enterprise/site-alpha/area-assembly/line-a/conveyor-01/running-state
virtual-enterprise/site-alpha/area-assembly/line-a/conveyor-01/motor-temperature
virtual-enterprise/site-alpha/area-assembly/line-a/conveyor-01/speed
virtual-enterprise/site-alpha/area-assembly/line-a/conveyor-01/speed-setpoint
virtual-enterprise/site-alpha/area-assembly/line-a/conveyor-01/fault-code
virtual-enterprise/site-alpha/area-assembly/line-a/conveyor-01/cycle-count
```

### Robot Arm 01

```
virtual-enterprise/site-alpha/area-assembly/line-a/robot-arm-01/running-state
virtual-enterprise/site-alpha/area-assembly/line-a/robot-arm-01/motor-temperature
virtual-enterprise/site-alpha/area-assembly/line-a/robot-arm-01/speed
virtual-enterprise/site-alpha/area-assembly/line-a/robot-arm-01/fault-code
virtual-enterprise/site-alpha/area-assembly/line-a/robot-arm-01/cycle-count
```

### Inspection Station 01

```
virtual-enterprise/site-alpha/area-assembly/line-a/inspection-station-01/running-state
virtual-enterprise/site-alpha/area-assembly/line-a/inspection-station-01/fault-code
virtual-enterprise/site-alpha/area-assembly/line-a/inspection-station-01/cycle-count
```

### Line A Aggregates

```
virtual-enterprise/site-alpha/area-assembly/line-a/line-state
virtual-enterprise/site-alpha/area-assembly/line-a/throughput-rate
virtual-enterprise/site-alpha/area-assembly/line-a/shift-count
```

---

## Assembly Area – Line B

### Conveyor 02

```
virtual-enterprise/site-alpha/area-assembly/line-b/conveyor-02/running-state
virtual-enterprise/site-alpha/area-assembly/line-b/conveyor-02/motor-temperature
virtual-enterprise/site-alpha/area-assembly/line-b/conveyor-02/speed
virtual-enterprise/site-alpha/area-assembly/line-b/conveyor-02/fault-code
```

### Welding Cell 01

```
virtual-enterprise/site-alpha/area-assembly/line-b/welding-cell-01/running-state
virtual-enterprise/site-alpha/area-assembly/line-b/welding-cell-01/motor-temperature
virtual-enterprise/site-alpha/area-assembly/line-b/welding-cell-01/speed
virtual-enterprise/site-alpha/area-assembly/line-b/welding-cell-01/fault-code
virtual-enterprise/site-alpha/area-assembly/line-b/welding-cell-01/cycle-count
```

### Conveyor 03

```
virtual-enterprise/site-alpha/area-assembly/line-b/conveyor-03/running-state
virtual-enterprise/site-alpha/area-assembly/line-b/conveyor-03/motor-temperature
virtual-enterprise/site-alpha/area-assembly/line-b/conveyor-03/speed
virtual-enterprise/site-alpha/area-assembly/line-b/conveyor-03/fault-code
```

---

## Packaging Area – Packaging Line 1

### Labeller 01

```
virtual-enterprise/site-alpha/area-packaging/line-pack-01/labeller-01/running-state
virtual-enterprise/site-alpha/area-packaging/line-pack-01/labeller-01/fault-code
virtual-enterprise/site-alpha/area-packaging/line-pack-01/labeller-01/speed
virtual-enterprise/site-alpha/area-packaging/line-pack-01/labeller-01/cycle-count
```

### Case Packer 01

```
virtual-enterprise/site-alpha/area-packaging/line-pack-01/case-packer-01/running-state
virtual-enterprise/site-alpha/area-packaging/line-pack-01/case-packer-01/fault-code
virtual-enterprise/site-alpha/area-packaging/line-pack-01/case-packer-01/speed
virtual-enterprise/site-alpha/area-packaging/line-pack-01/case-packer-01/cycle-count
```

### Palletiser 01

```
virtual-enterprise/site-alpha/area-packaging/line-pack-01/palletiser-01/running-state
virtual-enterprise/site-alpha/area-packaging/line-pack-01/palletiser-01/fault-code
virtual-enterprise/site-alpha/area-packaging/line-pack-01/palletiser-01/speed
virtual-enterprise/site-alpha/area-packaging/line-pack-01/palletiser-01/cycle-count
```

### Packaging Line 1 Aggregates

```
virtual-enterprise/site-alpha/area-packaging/line-pack-01/line-state
virtual-enterprise/site-alpha/area-packaging/line-pack-01/throughput-rate
virtual-enterprise/site-alpha/area-packaging/line-pack-01/shift-count
```

---

## Example Payload

```json
{
  "value": 47.3,
  "unit": "rpm",
  "quality": "Good",
  "timestamp": "2025-06-01T08:00:00Z",
  "source": "simulator"
}
```

See `/Docs/mqtt-topics.md` for full topic conventions, wildcard patterns, and QoS guidance.
