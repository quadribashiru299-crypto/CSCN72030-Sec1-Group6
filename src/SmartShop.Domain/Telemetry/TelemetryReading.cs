namespace SmartShop.Domain.Telemetry;

/// <summary>
/// A single sensor reading parsed from the ASCII simulation file.
/// This is the core telemetry data structure streamed to the HMI dashboard.
/// </summary>
public class TelemetryReading
{
    /// <summary>Which sensor produced this reading (matches the file's sensorId column).</summary>
    public string SensorId { get; set; } = "";

    /// <summary>UTC timestamp of the reading.</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>Numeric value. For bool sensors: 0 = normal, 1 = alarm/triggered.</summary>
    public double Value { get; set; }

    /// <summary>Unit of measurement: "C", "%", "count", "bool".</summary>
    public string Unit { get; set; } = "";

    /// <summary>Display label, e.g. "Store Temperature".</summary>
    public string Label { get; set; } = "";

    /// <summary>Classification against configured thresholds: "Normal" | "Warning" | "Critical".</summary>
    public string Status { get; set; } = "Normal";

    /// <summary>Optional textual state, e.g. "Active" / "Inactive" for the emergency alarm.</summary>
    public string? State { get; set; }

    /// <summary>False when the simulation file could not be read or validated (REQ-TEL-003).</summary>
    public bool Available { get; set; } = true;
}
