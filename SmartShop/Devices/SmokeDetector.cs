using SmartShop.Core;

namespace SmartShop.Devices;

/// <summary>Simulates a store smoke level. Thresholds apply to simulation data.</summary>
public sealed class SmokeDetector : SimulatedLevelSensor
{
    public const string SensorKey = "SmokeDetector";
    public const int DefaultDeviceId = 7;
    public const string DefaultUnit = "ppm";

    public SmokeDetector(string dataFilePath, int deviceId = DefaultDeviceId)
        : base(dataFilePath, SensorKey, DefaultUnit, new ThresholdConfig
        {
            HighWarning = 25,
            HighCritical = 50
        })
    {
        DeviceId = deviceId;
        DeviceName = "Smoke Detection";
    }
}
