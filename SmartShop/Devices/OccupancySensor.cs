namespace SmartShop.Devices;

/// <summary>Validates and retains the most recent store occupancy reading.</summary>
public sealed class OccupancySensor
{
    public const string DeviceId = "OccupancySensor";
    public const string DisplayName = "Store Occupancy";
    public const string Unit = "people";

    public int? LastKnownCount { get; private set; }
    public DateTimeOffset? LastValidAtUtc { get; private set; }

    /// <summary>
    /// Accepts a whole, nonnegative count supplied by the shared telemetry reader.
    /// An invalid reading leaves the last known value unchanged; the shared
    /// acquisition service is responsible for marking its quality Unavailable.
    /// </summary>
    public bool TryAcceptReading(double value, DateTimeOffset acquiredAtUtc)
    {
        if (!double.IsFinite(value) || value < 0 || value > int.MaxValue || value != Math.Truncate(value))
        {
            return false;
        }

        LastKnownCount = (int)value;
        LastValidAtUtc = acquiredAtUtc.ToUniversalTime();
        return true;
    }
}
