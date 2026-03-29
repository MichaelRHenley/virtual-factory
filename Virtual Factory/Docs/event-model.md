# Event Model

Equipment behavior is derived from state transitions instead of raw tag values.

Transitions tracked:

normal → alarm
alarm → normal
running → stopped
stopped → running

Each transition creates a record in:

EquipmentStateEvents

Example:

AlarmStart
AlarmClear
StopStart
StopEnd

These enable:

alarm duration tracking
stop duration tracking
MTTR calculation
alarm frequency analytics
machine severity scoring