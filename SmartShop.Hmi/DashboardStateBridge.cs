using SmartShop.Devices;

namespace SmartShop.Hmi;

public sealed record DeviceTile(
    string Id, string Name, string? Value, string? Unit, string Condition,
    string Quality, DateTimeOffset? ObservedAtUtc, bool LastKnown = false,
    string? CommandOutcome = null);

public sealed record DashboardSnapshot(
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyList<DeviceTile> Sensors,
    IReadOnlyList<DeviceTile> Actuators);

/// <summary>
/// The team's acquisition and command services call Publish... methods when their
/// shared contracts are ready. The web page only reads snapshots; it never reads
/// files or confirms commands itself.
/// </summary>
public sealed class DashboardStateBridge
{
    private static readonly (string Id, string Name)[] SensorCatalog =
    [
        ("StoreTemperatureSensor", "Store temperature"),
        ("RefrigeratorTemperature", "Refrigerator temperature"),
        ("ShelfStockSensor", "Shelf stock"),
        (OccupancySensor.DeviceId, OccupancySensor.DisplayName),
        ("CheckoutSensor", "Checkout activity"),
        ("ShockSensor", "Showcase shock"),
        ("SmokeDetector", "Smoke detection")
    ];

    private static readonly (string Id, string Name)[] ActuatorCatalog =
    [
        ("LightingController", "Lighting"),
        ("VentilationController", "Ventilation / HVAC"),
        (EntranceLockController.DeviceId, EntranceLockController.DisplayName),
        ("CheckoutStationController", "Checkout station"),
        ("AlarmController", "Alarm")
    ];

    private readonly object gate = new();
    private readonly OccupancySensor occupancy = new();
    private readonly EntranceLockController entranceLock = new();
    private readonly Dictionary<string, DeviceTile> otherTiles = new(StringComparer.Ordinal);
    private bool occupancyInvalid;
    private bool lockInvalid;
    private DateTimeOffset? lastLockSampleUtc;
    private string? lockOutcome;

    public bool PublishOccupancy(double value, string condition, DateTimeOffset acquiredAtUtc)
    {
        lock (gate)
        {
            var accepted = occupancy.TryAcceptReading(value, acquiredAtUtc);
            occupancyInvalid = !accepted;
            // Threshold classification belongs to the shared threshold service.
            if (accepted)
                occupancyCondition = ValidCondition(condition) ? condition : "Unknown";
            return accepted;
        }
    }

    private string occupancyCondition = "Unknown";

    public bool PublishEntranceLockFeedback(string? state, DateTimeOffset acquiredAtUtc)
    {
        lock (gate)
        {
            var accepted = entranceLock.TryAcceptBaseline(state, acquiredAtUtc);
            lockInvalid = !accepted;
            if (accepted) lastLockSampleUtc = acquiredAtUtc.ToUniversalTime();
            return accepted;
        }
    }

    /// <summary>Only a trusted command service calls this after authorization and confirmation.</summary>
    public bool PublishConfirmedLockCommand(string? state, DateTimeOffset confirmedAtUtc)
    {
        lock (gate)
        {
            var accepted = entranceLock.TryConfirmManualCommand(state, confirmedAtUtc);
            lockOutcome = accepted ? "Succeeded" : "Rejected";
            if (accepted)
            {
                lockInvalid = false;
                lastLockSampleUtc = confirmedAtUtc.ToUniversalTime();
            }
            return accepted;
        }
    }

    public void PublishLockOutcome(string outcome)
    {
        if (outcome is not ("Pending" or "Rejected" or "Failed" or "TimedOut"))
            throw new ArgumentException("Unsupported command outcome.", nameof(outcome));
        lock (gate)
        {
            lockOutcome = outcome;
            if (outcome == "TimedOut") lockInvalid = true;
        }
    }

    public void SimulationRestarted()
    {
        lock (gate) entranceLock.ClearManualOverride();
    }

    /// <summary>Other device owners publish their normalized, server-owned snapshots here.</summary>
    public void PublishSharedTile(DeviceTile tile)
    {
        if (tile.Id is OccupancySensor.DeviceId or EntranceLockController.DeviceId ||
            !SensorCatalog.Concat(ActuatorCatalog).Any(d => d.Id == tile.Id))
            throw new ArgumentException("Unknown or locally owned device ID.", nameof(tile));
        lock (gate) otherTiles[tile.Id] = tile;
    }

    public DashboardSnapshot GetSnapshot()
    {
        lock (gate)
        {
            var now = DateTimeOffset.UtcNow;
            var sensors = SensorCatalog.Select(d => d.Id == OccupancySensor.DeviceId
                ? OccupancyTile(now)
                : otherTiles.GetValueOrDefault(d.Id) ?? Empty(d.Id, d.Name)).ToArray();
            var actuators = ActuatorCatalog.Select(d => d.Id == EntranceLockController.DeviceId
                ? LockTile(now)
                : otherTiles.GetValueOrDefault(d.Id) ?? Empty(d.Id, d.Name)).ToArray();
            return new DashboardSnapshot(now, sensors, actuators);
        }
    }

    private DeviceTile OccupancyTile(DateTimeOffset now)
    {
        var last = occupancy.LastValidAtUtc;
        var quality = Quality(last, occupancyInvalid, now);
        return new DeviceTile(OccupancySensor.DeviceId, OccupancySensor.DisplayName,
            occupancy.LastKnownCount?.ToString(), OccupancySensor.Unit,
            quality == "Available" ? occupancyCondition : "Unknown", quality, last,
            quality != "Available" && last is not null);
    }

    private DeviceTile LockTile(DateTimeOffset now)
    {
        var last = entranceLock.LastConfirmedAtUtc;
        var quality = Quality(lastLockSampleUtc, lockInvalid, now);
        return new DeviceTile(EntranceLockController.DeviceId, EntranceLockController.DisplayName,
            entranceLock.LastKnownState?.ToString(), null, "N/A", quality, last,
            quality != "Available" && last is not null, lockOutcome);
    }

    private static string Quality(DateTimeOffset? last, bool invalid, DateTimeOffset now) =>
        invalid || last is null ? "Unavailable" :
        now - last.Value > TimeSpan.FromSeconds(3) ? "Stale" : "Available";

    private static bool ValidCondition(string? condition) =>
        condition is "Normal" or "Warning" or "Critical";

    private static DeviceTile Empty(string id, string name) =>
        new(id, name, null, null, "Unknown", "Unavailable", null);
}
