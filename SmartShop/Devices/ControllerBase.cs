using SmartShop.Models;

namespace SmartShop.Devices;

/// <summary>
/// Abstract base for all controllable actuators (lighting, ventilation,
/// checkout station, entrance lock, alarm). Concrete controllers override
/// <see cref="SendCommand"/> and <see cref="SupportedCommands"/> to define
/// which operations they accept.
/// </summary>
public abstract class ControllerBase : DeviceBase
{
    protected ControllerBase() => DeviceType = "Controller";

    /// <summary>Current state string, e.g. "ON", "OFF", "LOCKED", "INACTIVE", "SET:22".</summary>
    public string State { get; set; } = "OFF";

    /// <summary>The last command string that was accepted.</summary>
    public string? LastCommand { get; set; }

    /// <summary>When the last command was received (UTC).</summary>
    public DateTime? LastCommandAt { get; set; }

    /// <summary>List of command strings this controller accepts, e.g. ["ON","OFF"] for lighting.</summary>
    public abstract IEnumerable<string> SupportedCommands { get; }

    /// <summary>
    /// Applies a staff supervisory override. Returns true if the command was
    /// accepted and state changed; false if the command is not supported.
    /// Implementations must update <see cref="State"/> and <see cref="LastCommand"/>.
    /// </summary>
    public abstract bool SendCommand(CommandMessage command);
}
