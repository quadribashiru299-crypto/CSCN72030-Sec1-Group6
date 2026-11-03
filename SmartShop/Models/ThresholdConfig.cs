namespace SmartShop.Models;

/// <summary>
/// Threshold configuration for a single sensor. Values are read from
/// appsettings.json (DeviceThresholds section) so staff can tune them
/// without recompiling (REQ-STS-002).
/// </summary>
public class ThresholdConfig
{
    /// <summary>Warn when value goes above this.</summary>
    public double? WarnAbove { get; set; }

    /// <summary>Critical when value goes above this.</summary>
    public double? CritAbove { get; set; }

    /// <summary>Warn when value goes below this.</summary>
    public double? WarnBelow { get; set; }

    /// <summary>Critical when value goes below this.</summary>
    public double? CritBelow { get; set; }

    /// <summary>
    /// For binary sensors (smoke, shock, alarm): true means any value &gt;= 0.5
    /// is immediately Critical, ignoring the numeric above/below thresholds.
    /// </summary>
    public bool BoolCritical { get; set; }
}
