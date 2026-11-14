using System;
namespace SmartShop.Models;

/// <summary>
/// Status levels for sensor thresholds and UI alert styling (REQ-STS-001).
/// </summary>
public enum DeviceStatus
{
	Normal, 
	Warning,
	Critical
}

/// <summary>
/// Telemetry data packet sent across the system (REQ-INT-TEL-001).
/// Note: DeviceName is excluded here on purpose; static names are looked up in the HMI via DeviceId.
/// </summary>
public class TelemetryPayload
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
	public int DeviceId { get; init; }
	public double? Value { get; init; }
	public string Unit { get; init; }
    public DeviceStatus Status { get; init; }
    public bool IsDataAvailable { get; init; }

    /// <summary>
    /// Initializes a new telemetry data payload (REQ-INT-TEL-001)
    /// </summary>
    /// <param name="deviceId">Unique identifier of the device</param>
    /// <param name="value">Current or last known measured value</param>
    /// <param name="unit">Unit of measure (REQ-TEL-002)</param>
    /// <param name="status">Evaluated operational status (REQ-STS-001)</param>
    /// <param name="isDataAvailable">
    /// Flag indicating whether reading is fresh and valid. Defaults to true; 
    /// set to false on file read or validation errors (REQ-TEL-003, REQ-NFR-REL-001)
    /// </param>
    public TelemetryPayload(int deviceId, double? value, string unit, DeviceStatus status, bool isDataAvailable = true)
    {
        DeviceId = deviceId;
        Value = value;
        Unit = unit;
        Status = status;
        IsDataAvailable = isDataAvailable;
    }
}
