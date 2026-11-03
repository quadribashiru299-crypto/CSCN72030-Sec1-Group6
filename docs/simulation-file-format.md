# Simulation File Format Specification

**User Story:** US-TEL-01 — Telemetry Foundation
**Task:** Simulation file format spec
**Version:** 1.0 — October 2026

---

## 1. Purpose

The SmartShop SCADA system reads live sensor telemetry from a looped ASCII text
file (`mockdata.txt`). This document defines the exact format so every team
member can generate valid simulation data and every reader implementation can
parse it consistently.

## 2. File Location

```
data/mockdata.txt
```

The file is UTF-8 encoded, ASCII-compatible text. Lines are terminated by
CRLF or LF.

## 3. Line Format

Each non-comment line describes **one sensor reading** at a single point in
time. Fields are separated by the pipe character `|`:

```
timestamp|sensorId|value|unit|label
```

| Field | Type | Required | Description |
|---|---|---|---|
| `timestamp` | ISO 8601 UTC | Yes | e.g. `2026-10-14T08:00:00Z` |
| `sensorId` | string | Yes | Stable device ID (see table below) |
| `value` | double | Yes | Numeric reading; 0 or 1 for bool sensors |
| `unit` | string | Yes | `C`, `%`, `count`, or `bool` |
| `label` | string | Yes | Human-readable display name |

### Comment lines

Lines starting with `#` are comments and are skipped by the reader.
Blank lines are also skipped.

## 4. Time Steps

All devices report at the **same timestamp** within a time step. One time step
contains exactly **8 lines** (7 sensors + 1 alarm sensor). The next time step's
timestamp is exactly **5 seconds** later.

```
# Step 0 — 08:00:00
2026-10-14T08:00:00Z|StoreTemperatureSensor|21.0|C|Store Temperature
2026-10-14T08:00:00Z|RefrigeratorTemperature|2.9|C|Refrigerator Temperature
...
# Step 1 — 08:00:05
2026-10-14T08:00:05Z|StoreTemperatureSensor|20.8|C|Store Temperature
...
```

The reader advances **one time step per API call**. When it reaches the last
step it wraps around to the beginning, producing an infinite looped stream
(REQ-TEL-001).

## 5. Registered Device IDs

| sensorId | Unit | Direction | Warning threshold | Critical threshold |
|---|---|---|---|---|
| `StoreTemperatureSensor` | C | above | > 23 | > 26 |
| `RefrigeratorTemperature` | C | above | > 4 | > 5 |
| `CheckoutSensor` | count | — | (per checkout flow) | — |
| `ShelfStockSensor` | % | below | < 20 | < 10 |
| `OccupancySensor` | count | above | > 15 | > 20 |
| `ShockSensor` | bool | — | — | = 1 (any trigger) |
| `SmokeDetector` | bool | — | — | = 1 (any trigger) |
| `AlarmSensor` | bool | — | — | = 1 (Active) |

## 6. Parsing Rules

1. Trim whitespace from every line.
2. Skip empty lines and lines starting with `#`.
3. Split on `|` — must have at least 4 fields; field 5 (label) falls back to `sensorId` if absent.
4. Parse `timestamp` with `DateTimeStyles.AssumeUniversal, InvariantCulture`.
5. Parse `value` with `NumberStyles.Float, InvariantCulture`.
6. If any line fails to parse, **skip that line** (do not crash the reader).
7. After loading, sort all records by `(timestamp, sensorId)`.
8. Count distinct sensor IDs to determine `devicesPerStep`.

## 7. Failure Behaviour

If the file is missing, empty, or contains no valid records, the reader must
**keep running** and return all 8 devices with `Available = false`. The HMI
dashboard shows "unavailable" cards instead of crashing (REQ-NFR-REL-001).
