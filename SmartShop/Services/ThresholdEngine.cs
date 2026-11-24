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
    /// <summary>
    /// Classifies one reading against the threshold set supplied by the caller.
    /// The engine holds no state: every sensor passes its own configuration in.
    /// </summary>
    public DeviceStatus Classify(ThresholdConfig config, double value)
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
