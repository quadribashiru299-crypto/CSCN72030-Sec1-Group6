using SmartShop.Models;

namespace SmartShop.Devices;

/// <summary>
/// Abstract base for all read-only sensors. Concrete sensors (StoreTemperature,
/// RefrigeratorTemperature, ShelfStock, Occupancy, Checkout, Shock, Smoke, Alarm)
/// override <see cref="Read"/> to produce a <see cref="TelemetryReading"/> and
/// <see cref="Classify"/> to map a raw value to Normal / Warning / Critical.
/// </summary>
public abstract class SensorBase : DeviceBase
{
    protected SensorBase() => DeviceType = "Sensor";

    /// <summary>Measurement unit: "C", "%", "count", "bool".</summary>
    public string Unit { get; set; } = "";

    /// <summary>Most recent raw value (null if unavailable).</summary>
    public double? CurrentValue { get; set; }

    /// <summary>Threshold configuration used by <see cref="Classify"/>.</summary>
    public ThresholdConfig? Thresholds { get; set; }

    /// <summary>
    /// Produces the next telemetry reading. The simulation cursor advances
    /// one time step per call; the looped file wraps around at the end.
    /// </summary>
    public abstract TelemetryReading Read();

    /// <summary>
    /// Maps a raw numeric value to a status string: "Normal", "Warning", or "Critical"
    /// (REQ-STS-001). Binary sensors (shock, smoke, alarm) treat value &gt;= 0.5 as Critical.
    /// </summary>
    public virtual string Classify(double value)
    {
        if (Thresholds is null) return "Normal";

        if (Thresholds.BoolCritical)
            return value >= 0.5 ? "Critical" : "Normal";

        if ((Thresholds.CritAbove.HasValue && value > Thresholds.CritAbove.Value) ||
            (Thresholds.CritBelow.HasValue && value < Thresholds.CritBelow.Value))
            return "Critical";

        if ((Thresholds.WarnAbove.HasValue && value > Thresholds.WarnAbove.Value) ||
            (Thresholds.WarnBelow.HasValue && value < Thresholds.WarnBelow.Value))
            return "Warning";

        return "Normal";
    }
}
