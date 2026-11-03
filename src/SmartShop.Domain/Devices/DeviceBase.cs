namespace SmartShop.Domain.Devices;

/// <summary>
/// Abstract base for every device in the store — both sensors (read-only)
/// and controllers/actuators (writable). All concrete devices inherit from
/// this class so the HMI dashboard can treat them uniformly.
/// </summary>
public abstract class DeviceBase
{
    /// <summary>Stable identifier, e.g. "StoreTemperatureSensor" or "lighting".</summary>
    public string Id { get; set; } = "";

    /// <summary>Human-friendly display name, e.g. "Store Temperature".</summary>
    public string Name { get; set; } = "";

    /// <summary>Device category: "Sensor" or "Controller".</summary>
    public string DeviceType { get; set; } = "";

    /// <summary>False when the simulation file could not be read (REQ-TEL-003).</summary>
    public bool IsOnline { get; set; } = true;

    /// <summary>Last time the device produced a reading or accepted a command.</summary>
    public DateTime LastUpdated { get; set; }
}
