namespace SmartShop.Devices;

/// <summary>
/// Abstract base for all controllable actuators (lighting, ventilation,
/// checkout station, entrance lock, alarm). Concrete controllers inherit
/// from this class. The command message structure is defined separately
/// by the Telemetry module owner.
/// </summary>
public abstract class ControllerBase : DeviceBase
{
    protected ControllerBase() => DeviceType = "Controller";

    /// <summary>Current state string, e.g. "ON", "OFF", "LOCKED", "INACTIVE".</summary>
    public string State { get; set; } = "OFF";

    /// <summary>The last command string that was accepted.</summary>
    public string? LastCommand { get; set; }

    /// <summary>When the last command was received (UTC).</summary>
    public DateTime? LastCommandAt { get; set; }
}
