using SmartShop.Core;
using SmartShop.Models;

namespace SmartShop.Services;

/// <summary>
/// Central threshold classification shared by every sensor.
/// Same rule set as <see cref="Sensor.ClassifyStatus"/>: the critical band is
/// evaluated before the warning band, and bounds are inclusive (a value exactly
/// equal to a bound triggers that band).
/// Pure managed code - no UI, no database, no configuration framework.
/// </summary>
public sealed class ThresholdEngine
{
    private readonly IReadOnlyDictionary<string, ThresholdConfig> _thresholds;

    public ThresholdEngine(IReadOnlyDictionary<string, ThresholdConfig> thresholds)
    {
        _thresholds = thresholds;
    }

    /// <summary>
    /// Classifies a reading for the named sensor. Sensors without a configured
    /// threshold set fall back to <see cref="DeviceStatus.Normal"/>.
    /// </summary>
    public DeviceStatus Classify(string sensorId, double value)
    {
        var config = _thresholds.TryGetValue(sensorId, out var found) ? found : default;
        return Classify(config, value);
    }

    /// <summary>Pure classification against a single threshold set.</summary>
    public static DeviceStatus Classify(ThresholdConfig config, double value)
    {
        if ((config.LowCritical is double lowCritical && value <= lowCritical) ||
            (config.HighCritical is double highCritical && value >= highCritical))
        {
            return DeviceStatus.Critical;
        }

        if ((config.LowWarning is double lowWarning && value <= lowWarning) ||
            (config.HighWarning is double highWarning && value >= highWarning))
        {
            return DeviceStatus.Warning;
        }

        return DeviceStatus.Normal;
    }
}
