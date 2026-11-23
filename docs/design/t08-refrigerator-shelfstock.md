# T08 — RefrigeratorTemperature & ShelfStockSensor — Detailed Design

| Field | Value |
|---|---|
| Task | T08 — Requirements definition and detailed design (M1) |
| Owner | Weiming |
| Due | Nov 11, 2026 |
| Implementation task | T16 — Implement RefrigeratorTemperature + ShelfStockSensor (M2, Nov 20) |
| Depends on | T03 (`feature/t03-telemetry-threshold-engine`, PR #13) for the simulation record contract and the looped reader in `Sensor` |
| Status | Ready for implementation |

---

## 1. Purpose and scope

This document specifies the two device classes owned by Weiming:

- `RefrigeratorTemperature` — the cold-chain temperature sensor (REQ-TEL-005).
- `ShelfStockSensor` — the shelf stock-level sensor (REQ-TEL-007).

Both are monitored sensors: they read a looped ASCII file, publish a
`TelemetryPayload`, and are classified into `Normal` / `Warning` / `Critical`
against configurable thresholds (REQ-STS-001, REQ-STS-002).

Out of scope for T08: the MVC dashboard tiles (T21), the SQLite logging pipeline
(T23), and the kiosk purchase flow (T24). This design only defines the device
objects and their data contract.

---

## 2. Traceability

| Requirement | How this design satisfies it |
|---|---|
| REQ-TEL-001 | Both classes read a looped ASCII file through the shared reader. |
| REQ-TEL-002 | `GetTelemetry()` publishes `Value`, `Unit`, `Status` and `LastUpdated` on every poll. |
| REQ-TEL-003 | A missing file or invalid record sets `IsDataAvailable = false` and keeps the last known `Value`. |
| REQ-TEL-005 | `RefrigeratorTemperature` publishes the refrigerator temperature in `C`. |
| REQ-TEL-007 | `ShelfStockSensor` publishes the remaining shelf stock as a percentage. |
| REQ-STS-001 | `ClassifyStatus()` maps the value to one of three states. |
| REQ-STS-002 | Thresholds are injected through the constructor; the defaults live in constants. |
| REQ-INT-TEL-001 | Both publish the shared `TelemetryPayload` structure. |
| REQ-NFR-REL-001 | Neither reader throws; failures are reported as unavailable data. |
| REQ-NFR-MNT-001 | Both reuse the base-class reader; no duplicate parsing code. |

---

## 3. Common design

### 3.1 Inheritance

```
Device (abstract)
└── Sensor (abstract)          // looped ASCII reading + ClassifyStatus
    ├── RefrigeratorTemperature
    └── ShelfStockSensor
```

Both classes inherit `Sensor` and implement only `Validate()`. The reading,
cursor wrap, record rejection and payload construction stay in the base class
(T03), so a threshold change never touches a device class and a device class
never re-implements parsing.

### 3.2 Simulation record contract

One record per line, exactly three `|`-separated fields (T03 spec):

```
device|value|unit
```

| Field | RefrigeratorTemperature | ShelfStockSensor |
|---|---|---|
| `device` | `RefrigeratorTemperature` | `ShelfStockSensor` |
| `value` | temperature in `C` | remaining stock in `%` |
| `unit` | `C` | `%` |

Rules inherited from the base class: blank lines and `#` comments are skipped,
`device` must match the class simulation key (case-insensitive), the record must
have exactly three fields, and the cursor wraps at the end of the file.

### 3.3 Device identifiers

| Device | DeviceId | DeviceName | Unit | Source file |
|---|---|---|---|---|
| `RefrigeratorTemperature` | 2 | `Refrigerator Temperature` | `C` | `Data/refrigerator_temperature.txt` |
| `ShelfStockSensor` | 4 | `Shelf Stock Level` | `%` | `Data/shelf_stock_sensor.txt` |

The IDs follow the SRS device list order (sensors 1–7, controllers 11–12) and
match the demo project's identifiers. Both data files are added by T03
(PR #13); `SmartShop.csproj` already copies `Data\**\*.txt` to the output
folder, so no build change is needed.

### 3.4 Threshold injection (REQ-STS-002)

Thresholds are not hard-coded inside the classification path. Each constructor
takes an optional `ThresholdConfig`; when it is omitted the well-known defaults
below are used:

```csharp
public RefrigeratorTemperature(
    string dataFilePath,
    int deviceId = DefaultDeviceId,
    ThresholdConfig? thresholds = null)
```

This keeps the defaults discoverable as constants, lets tests exercise any band
without touching production code, and gives the dashboard/administrator path a
seam to supply tuned thresholds (T30) without redesigning the class.

---

## 4. RefrigeratorTemperature

### 4.1 Class shape

```csharp
namespace SmartShop.Devices;

public class RefrigeratorTemperature : Sensor
{
    public const string SensorKey = "RefrigeratorTemperature";
    public const int    DefaultDeviceId = 2;
    public const string DefaultUnit = "C";

    public const double DefaultLowCritical  = 2.0;   // <= 2.0  Critical (too cold)
    public const double DefaultLowWarning   = 3.0;   // <= 3.0  Warning
    public const double DefaultHighWarning  = 4.0;   // >= 4.0  Warning
    public const double DefaultHighCritical = 5.0;   // >= 5.0  Critical (cold chain broken)

    public const double MinPlausibleTemperature = -10.0;
    public const double MaxPlausibleTemperature = 20.0;

    public RefrigeratorTemperature(string dataFilePath, int deviceId = DefaultDeviceId,
                                   ThresholdConfig? thresholds = null);
    public override bool Validate();
}
```

### 4.2 Constructor behaviour

1. `SimulationFilePath = dataFilePath` (throws `ArgumentNullException` when null).
2. `DeviceId`, `DeviceName = "Refrigerator Temperature"`, `Unit = "C"`.
3. `Thresholds = thresholds ?? new ThresholdConfig { ...defaults... }`.
4. `IsDataAvailable = false`, `Status = DeviceStatus.Warning` — a sensor that has
   not produced a reading yet must not look healthy on the dashboard.

### 4.3 Validation rule

```csharp
public override bool Validate()
{
    bool valid = CandidateValue is double t
              && double.IsFinite(t)
              && t >= MinPlausibleTemperature
              && t <= MaxPlausibleTemperature;
    IsDataAvailable = valid;
    return valid;
}
```

Rationale:

- `IsFinite` rejects `NaN` / `±∞` produced by a corrupted file.
- The ±plausible window rejects physically impossible values (`-273`, `9999`).
  It is deliberately wide enough that any real cold-chain excursion still passes
  and is classified rather than discarded.

### 4.4 Status mapping

Critical is evaluated before Warning and both bands are inclusive, so a value
exactly on a line takes the stronger band.

| Value | Band hit | Status |
|---|---|---|
| ≥ 5.0 | `HighCritical` | Critical |
| 4.0 – 4.99 | `HighWarning` | Warning |
| 3.01 – 3.99 | none | Normal |
| 2.01 – 3.0 | `LowWarning` | Warning |
| ≤ 2.0 | `LowCritical` | Critical |

Sample from `refrigerator_temperature.txt`:

| Record | Value | Status |
|---|---|---|
| 1 | 3.6 | Normal |
| 3 | 4.0 | Warning (exactly on `HighWarning`) |
| 5 | 5.2 | Critical |
| 8 | 3.0 | Warning (exactly on `LowWarning`) |
| 10 | 1.8 | Critical |

### 4.5 Test matrix (for T16)

| # | Case | Expected |
|---|---|---|
| 1 | First read of the sample file | `3.6`, Normal, available |
| 2 | Read the record holding `4.0` | Warning |
| 3 | Read the record holding `5.2` | Critical |
| 4 | Value `2.0` (temporary one-line data file) | Critical (inclusive lower bound) |
| 5 | Read the record holding `3.0` | Warning (inclusive lower bound) |
| 6 | Poll 13 times | Cursor wraps to record 1, no EOF |
| 7 | Missing file | No exception, unavailable, no value published |
| 8 | Record with a foreign device key | Rejected, unavailable |
| 9 | Record with `n/a` as value | Rejected, unavailable |
| 10 | Value outside the plausible window | Rejected, unavailable |
| 11 | `GetTelemetry()` | DeviceId 2, unit `C`, timestamp set |
| 12 | Injected thresholds | Custom bands are used instead of defaults |

---

## 5. ShelfStockSensor

### 5.1 Class shape

```csharp
namespace SmartShop.Devices;

public class ShelfStockSensor : Sensor
{
    public const string SensorKey = "ShelfStockSensor";
    public const int    DefaultDeviceId = 4;
    public const string DefaultUnit = "%";

    public const double DefaultLowCritical = 10.0;   // <= 10  Critical (shelf nearly empty)
    public const double DefaultLowWarning  = 20.0;   // <= 20  Warning  (restock soon)

    public const double MinPlausibleStock = 0.0;
    public const double MaxPlausibleStock = 100.0;

    public ShelfStockSensor(string dataFilePath, int deviceId = DefaultDeviceId,
                            ThresholdConfig? thresholds = null);
    public override bool Validate();
}
```

Only the low bands are configured. High stock is never a problem, so
`HighWarning` / `HighCritical` stay `null` and the engine always returns
`Normal` from that side.

### 5.2 Constructor behaviour

Same pattern as `RefrigeratorTemperature`:

- `DeviceName = "Shelf Stock Level"`, `Unit = "%"`.
- Defaults `LowWarning = 20`, `LowCritical = 10`, high bands unset.
- Starts unavailable with `Status = Warning`.

### 5.3 Validation rule

```csharp
public override bool Validate()
{
    bool valid = CandidateValue is double pct
              && double.IsFinite(pct)
              && pct >= MinPlausibleStock
              && pct <= MaxPlausibleStock
              && pct == Math.Floor(pct);
    IsDataAvailable = valid;
    return valid;
}
```

Rationale:

- The field is a percentage of capacity, so `0…100` is the only meaningful range.
- `pct == Math.Floor(pct)` keeps the dashboard reading in whole percent units and
  rejects half-parsed records such as `73.5` that the file should not contain.
  The records in `shelf_stock_sensor.txt` are all whole numbers.

### 5.4 Status mapping

| Value | Band hit | Status |
|---|---|---|
| 21 – 100 | none | Normal |
| 10.01 – 20 | `LowWarning` | Warning |
| ≤ 10 | `LowCritical` | Critical |

Sample from `shelf_stock_sensor.txt`:

| Record | Value | Status |
|---|---|---|
| 1 | 92 | Normal |
| 8 | 20 | Warning (exactly on `LowWarning`) |
| 11 | 9 | Critical |
| 12 | 4 | Critical |

### 5.5 Test matrix (for T16)

| # | Case | Expected |
|---|---|---|
| 1 | First read | `92`, Normal, unit `%` |
| 2 | Record holding `20` | Warning (inclusive) |
| 3 | Record holding `9` | Critical |
| 4 | Value `0` | Critical, still valid data |
| 5 | Value `100` | Normal |
| 6 | Value `101` | Rejected, unavailable |
| 7 | Value `-1` | Rejected, unavailable |
| 8 | Value `73.5` | Rejected (not a whole percent) |
| 9 | Poll full loop | Cursor wraps correctly |
| 10 | Missing file | Unavailable, no exception |
| 11 | Foreign device key | Rejected |
| 12 | Injected thresholds | Custom bands applied |

---

## 6. Failure behaviour (both classes)

| Situation | `IsDataAvailable` | `Value` | `Status` | Exception |
|---|---|---|---|---|
| File missing / unreadable | `false` | unchanged (last known) | Warning | none |
| File has no parsable record for this device | `false` | unchanged | Warning | none |
| Current record has a foreign key | `false` | unchanged | Warning | none |
| Value not numeric | `false` | unchanged | Warning | none |
| Value outside the plausible window | `false` | unchanged | Warning | none |
| File becomes readable again | `true` | next valid value | recalculated | none |

`LastSimulationError` (base class) carries the reason for the EventLog; the
dashboard uses `IsDataAvailable` to grey the tile and label it "last known".

---

## 7. Collaboration with the kiosk purchase simulator (T24)

`feature/t24-kiosk-purchase-simulator` defines a `Core/IShelfStock` contract:

```csharp
bool TryRemoveStock(string itemId, int quantity, out string? errorMessage);
```

Two different notions of "stock" meet here:

1. **This sensor** reports an aggregate percentage of shelf capacity, one value
   per poll, driven by the simulation file.
2. **The kiosk** needs per-item quantities (`MILK-1L`, `BREAD`, …) that decrease
   when a simulated purchase happens.

A percentage cannot answer "is there milk left". **Design decision:** T16 does
not implement `IShelfStock`. The percentage sensor stays a pure monitor; the
per-item stock table belongs to the kiosk domain (an in-memory inventory
repository owned by T24, exactly like the `FakeShelf` in its tests).

If the team decides the kiosk must write through this sensor, the follow-up
design adds an item-level store inside `ShelfStockSensor` plus a rule for
recomputing the published percentage from it. That is a separate change and
must not be smuggled into T16.

---

## 8. Open decisions

| # | Question | Current assumption | Owner |
|---|---|---|---|
| 1 | Confirm `RefrigeratorTemperature` unit string (`C` vs `°C`) | `C`, matching the existing data file and `StoreTemperatureSensor` | Weiming / Kristyna |
| 2 | Confirm device IDs 2 and 4 against the SRS device list | Accepted from the demo project ordering | Team |
| 3 | Should low-critical for refrigerator be `2` (near freezing) or `0`? | `2` — produce quality degrades before freezing | Weiming (tune in T30) |
| 4 | Whole-percent rule for shelf stock | Enforced; relax only if the data source becomes fractional | Weiming |
| 5 | Per-item inventory for the kiosk | Out of T16; belongs to T24 | Bashiru / Weiming |

---

## 9. Implementation checklist (T16)

1. Create `SmartShop/Devices/RefrigeratorTemperature.cs` and `ShelfStockSensor.cs`
   following sections 4 and 5.
2. Confirm `Data/refrigerator_temperature.txt` and `Data/shelf_stock_sensor.txt`
   are copied to the build output (already configured in `SmartShop.csproj`).
3. Add `RefrigeratorTemperatureTests` and `ShelfStockSensorTests` covering the
   matrices above (one test per row, plus the loop and failure cases).
4. Run `dotnet test` locally; all tests green and zero warnings before opening
   the T16 PR.
