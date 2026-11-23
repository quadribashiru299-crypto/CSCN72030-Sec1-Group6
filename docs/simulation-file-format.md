# ASCII Simulation File Format

Specification for the plain-text data files that drive the simulated SmartShop
devices. Every simulated sensor or controller reads its own file, one record per
poll, and loops forever (T03).

## File location and naming

- One file per simulated device, stored under `SmartShop/Data/`.
- File name is descriptive, not authoritative: e.g. `occupancy_sensor.txt`,
  `entrance_lock.txt`. The binding to a device is made by the `device` field inside
  the records, not by the file name.
- Files are copied to the build output, so readers can resolve them relative to the
  application directory: `<None Update="Data\**\*.txt" CopyToOutputDirectory="PreserveNewest" />`
  in `SmartShop/SmartShop.csproj`.

## Line format

```
# a comment line, ignored
device|value|unit
OccupancySensor|8|count
```

Fields, in order, separated by a single `|`:

| # | Field | Required | Type | Meaning |
|---|-------|----------|------|---------|
| 1 | `device` | yes | string | Simulation key of the producing device; must match the reading class (case-insensitive). |
| 2 | `value` | yes | floating point | Measured value, parsed with the invariant culture (`.` decimal separator). |
| 3 | `unit` | yes | string | Unit of measure (REQ-TEL-002), e.g. `C`, `%`, `count`, `bool`. |

Records carry no timestamp: the file is a loop, so a replayed record is treated
as fresh and the reader stamps `LastUpdated` with the current UTC time when it
successfully consumes the record.

A record must contain **exactly three** `|`-separated fields. Fewer means the
record is incomplete; more means an unsupported column is present. Either way the
record is rejected.

## Parsing rules

1. The file is read line by line. Empty lines and lines whose first non-space
   character is `#` are skipped.
2. Each remaining line is split on `|`. Fields are trimmed of surrounding
   whitespace.
3. `device` must equal the reading device's simulation key
   (`Sensor.SimulationKey`, which defaults to the concrete class name, e.g.
   `ShelfStockSensor`). Records for another device are ignored by this device and
   never contributed to its value stream.
4. `value` must parse as a `double` under the invariant culture. A value that does
   not parse invalidates the record; the device keeps its last known value.
5. `unit` is not validated by the base contract. Devices may tighten it
   (see Device-specific conventions).

## Loop semantics

The file is a closed loop, not a finite log (REQ-TEL-001):

- Each read consumes exactly one record and advances a cursor by one position.
- When the cursor passes the last record it wraps to the first record.
- Therefore a device polled indefinitely keeps producing readings without
  reaching an end, and no EOF state is ever surfaced to the caller.
- The cursor is per-device instance state; two instances of the same device each
  walk the file independently.

## Error handling

A malformed or missing file must never crash the application
(REQ-NFR-REL-001). Behaviour:

| Situation | Result |
|-----------|--------|
| File missing, unreadable, or access denied | `IsDataAvailable = false`, status `Warning`, last known value retained, reason recorded, no exception thrown |
| File present but contains no usable record for this device | Same as above |
| Current record has an invalid device key or value | That record is rejected, `IsDataAvailable = false`, last known value retained |
| File becomes readable later | The reader retries on the next poll and resumes normally |

`IsDataAvailable = false` is the signal to the HMI that the displayed value is
stale; the payload still carries the previous value so the UI can grey it out
instead of blanking the tile.

## Device-specific conventions

| Device | Unit accepted | Value rule | Status mapping |
|--------|---------------|------------|----------------|
| `OccupancySensor` | `count` or `people` | finite, non-negative, whole number | threshold bands: `HighWarning`, `HighCritical` |
| `EntranceLockController` | `bool` | `1` = `Locked`, `0` = `Unlocked`, anything else invalid | `Normal` when a state is confirmed |
| Threshold sensors (`RefrigeratorTemperature`, `ShelfStockSensor`, …) | declared in the constructor, e.g. `C` / `count` | finite; range validated by the device's `Validate()` | `LowCritical` / `LowWarning` / `HighWarning` / `HighCritical`, both bands inclusive |

## Example file

```
# SmartShop OccupancySensor looped simulation data
# Format: device|value|unit
OccupancySensor|8|count
OccupancySensor|14|count
OccupancySensor|15|count
OccupancySensor|18|count
OccupancySensor|20|count
```

Polling this file four times yields `8` (`Normal`), `14` (`Normal`), `15`
(`Warning`) and `18` (`Warning`); a fifth poll wraps back to `8`.

## Where it is implemented

| Rule | Location |
|------|----------|
| Split, skip comments, parse device/value | `SmartShop/Core/Sensor.cs` → `ParseValue` |
| Cursor advance and wrap-around | `SmartShop/Core/Sensor.cs` → `NextSimulationRecord` |
| Read on demand from `GetTelemetry()` | `SmartShop/Core/Sensor.cs` → `GetTelemetry` |
| Threshold band evaluation | `SmartShop/Services/ThresholdEngine.cs`, `Sensor.ClassifyStatus` |
| Invalid input handling | `Sensor.MarkUnavailable`, `LastSimulationError` |
