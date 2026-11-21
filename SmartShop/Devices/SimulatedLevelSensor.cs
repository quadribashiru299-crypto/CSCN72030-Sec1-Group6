using SmartShop.Core;
using SmartShop.Models;

namespace SmartShop.Devices;

/// <summary>
/// Reads a nonnegative measurement from a looped simulation file and retains the last valid sample.
/// </summary>
public abstract class SimulatedLevelSensor : Sensor
{
    private readonly LoopedDeviceFile _source;
    private readonly string _sensorKey;
    private double? _candidateValue;

    public string? LastReadError { get; private set; }

    protected SimulatedLevelSensor(string dataFilePath, string sensorKey, string unit, ThresholdConfig thresholds)
    {
        _source = new LoopedDeviceFile(dataFilePath);
        _sensorKey = sensorKey;
        Unit = unit;
        Thresholds = thresholds;
        Status = DeviceStatus.Warning;
    }

    public override void ReadData()
    {
        bool read = _source.TryRead(_sensorKey, Unit, out DateTime timestamp, out double value, out string? error);
        _candidateValue = read ? value : null;

        if (Validate())
        {
            Value = _candidateValue;
            LastUpdated = timestamp;
            LastReadError = null;
        }
        else
        {
            LastReadError = error ?? $"Invalid {_sensorKey} reading: values must be nonnegative.";
        }

        ClassifyStatus();
    }

    public override bool Validate()
    {
        IsDataAvailable = _candidateValue >= 0;
        return IsDataAvailable;
    }

    public override TelemetryPayload GetTelemetry() =>
        new(DeviceId, Value, Unit, Status, IsDataAvailable) { Timestamp = LastUpdated };
}
