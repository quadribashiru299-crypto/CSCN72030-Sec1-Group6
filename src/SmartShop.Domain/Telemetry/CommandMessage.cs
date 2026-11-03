namespace SmartShop.Domain.Telemetry;

/// <summary>
/// A staff supervisory override command sent to a controller/actuator.
/// This is the core command data structure.
/// </summary>
public class CommandMessage
{
    /// <summary>Which controller to target (e.g. "lighting", "alarm", "entrancelock").</summary>
    public string TargetId { get; set; } = "";

    /// <summary>
    /// Command string. Common values: "ON", "OFF", "LOCK", "UNLOCK",
    /// "ACTIVATE", "DEACTIVATE", "OPEN", "CLOSE", "SET:x" (e.g. "SET:22").
    /// </summary>
    public string Command { get; set; } = "";

    /// <summary>Staff ID who issued the command (for audit trail / RBAC).</summary>
    public string? IssuedBy { get; set; }

    /// <summary>When the command was issued (UTC).</summary>
    public DateTime Timestamp { get; set; }
}
