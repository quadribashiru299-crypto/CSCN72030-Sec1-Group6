namespace SmartShop.Devices;

/// <summary>
/// Abstract base for all read-only sensors. Concrete sensors (StoreTemperature,
/// RefrigeratorTemperature, ShelfStock, Occupancy, Checkout, Shock, Smoke, Alarm)
/// inherit from this class. The telemetry reading structure and threshold
/// classification are defined separately by the Telemetry module owner.
/// </summary>
public abstract class SensorBase : DeviceBase
{
    protected SensorBase() => DeviceType = "Sensor";

    /// <summary>Measurement unit: "C", "%", "count", "bool".</summary>
    public string Unit { get; set; } = "";

    /// <summary>Most recent raw value (null if unavailable).</summary>
    public double? CurrentValue { get; set; }
}
