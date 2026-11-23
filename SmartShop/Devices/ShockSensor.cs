using SmartShop.Core;

namespace SmartShop.Devices;

/// <summary>Simulates the magnitude of a shock or vibration at a product showcase.</summary>
public sealed class ShockSensor : SimulatedLevelSensor
{
    public const string SensorKey = "ShockSensor";
    public const int DefaultDeviceId = 6;
    public const string DefaultUnit = "g";

    public ShockSensor(string dataFilePath, int deviceId = DefaultDeviceId)
        : base(dataFilePath, SensorKey, DefaultUnit, new ThresholdConfig
        {
            HighWarning = 1,
            HighCritical = 2
        })
    {
        DeviceId = deviceId;
        DeviceName = "Showcase Shock";
    }
}
