using System.Globalization;
using Microsoft.Extensions.Configuration;
using SmartShop.Core;
using SmartShop.Models;

namespace SmartShop.Services;

/// <summary>
/// One parsed line from the looped ASCII simulation file.
/// Format is defined in docs/simulation-file-format.md.
/// </summary>
public sealed record TelemetrySample(
    DateTime Timestamp,
    string SensorId,
    double Value,
    string Unit,
    string Label,
    DeviceStatus Status,
    bool IsDataAvailable);

/// <summary>
/// Reads the looped ASCII simulation file (Data/mockdata.txt) and returns
/// exactly one time step per call. The cursor wraps around, so the stream is
/// infinite (REQ-TEL-001).
/// </summary>
public sealed class MockDataService
{
    /// <summary>Registered device list from the spec, used when the file is missing.</summary>
    private static readonly (string Id, string Unit, string Label)[] DefaultDevices =
    {
        ("StoreTemperatureSensor", "C", "Store Temperature"),
        ("RefrigeratorTemperature", "C", "Refrigerator Temperature"),
        ("CheckoutSensor", "count", "Checkout Activity"),
        ("ShelfStockSensor", "%", "Shelf Stock Level"),
        ("OccupancySensor", "count", "Store Occupancy"),
        ("ShockSensor", "bool", "Shock / Vibration"),
        ("SmokeDetector", "bool", "Smoke Detector"),
        ("AlarmSensor", "bool", "Emergency Alarm"),
    };

    private readonly List<TelemetrySample> _records = new();
    private readonly Dictionary<string, ThresholdConfig> _thresholds;
    private int _cursor;
    private int _devicesPerStep = DefaultDevices.Length;

    public MockDataService(string dataFilePath, IConfiguration config)
    {
        _thresholds = ReadThresholds(config);

        if (File.Exists(dataFilePath))
        {
            foreach (var raw in File.ReadAllLines(dataFilePath))
            {
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
                    continue;

                var parts = line.Split('|');
                if (parts.Length < 4)
                    continue;

                if (DateTime.TryParse(parts[0], CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal, out var ts) &&
                    double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                {
                    var sensorId = parts[1].Trim();
                    var unit = parts[3].Trim();
                    var label = parts.Length > 4 ? parts[4].Trim() : sensorId;

                    _records.Add(new TelemetrySample(
                        ts, sensorId, value, unit, label, Classify(sensorId, value), true));
                }
            }
        }

        _records.Sort((a, b) =>
        {
            var byTime = a.Timestamp.CompareTo(b.Timestamp);
            return byTime != 0 ? byTime : string.CompareOrdinal(a.SensorId, b.SensorId);
        });

        var distinctDevices = _records.Select(r => r.SensorId).Distinct().Count();
        if (distinctDevices > 0)
            _devicesPerStep = distinctDevices;
    }

    /// <summary>True when the simulation file produced at least one valid reading.</summary>
    public bool IsAvailable => _records.Count > 0;

    /// <summary>Returns the next time step and advances the cursor by one step.</summary>
    public IReadOnlyList<TelemetrySample> GetNextBatch()
    {
        if (_records.Count == 0)
            return UnavailableBatch();

        var batch = new List<TelemetrySample>(_devicesPerStep);
        for (var i = 0; i < _devicesPerStep; i++)
            batch.Add(_records[(_cursor + i) % _records.Count]);

        // Advance one time step; wrap so the loop is infinite (REQ-TEL-001).
        _cursor = (_cursor + _devicesPerStep) % _records.Count;
        return batch;
    }

    /// <summary>All 8 registered devices reported as unavailable (REQ-NFR-REL-001).</summary>
    private static IReadOnlyList<TelemetrySample> UnavailableBatch()
    {
        var now = DateTime.UtcNow;
        return DefaultDevices
            .Select(d => new TelemetrySample(now, d.Id, 0d, d.Unit, d.Label, DeviceStatus.Warning, false))
            .ToList();
    }

    /// <summary>
    /// Same rule set as <see cref="Sensor.ClassifyStatus"/>: critical bands are
    /// checked first, and thresholds are inclusive (value == threshold triggers).
    /// </summary>
    private DeviceStatus Classify(string sensorId, double value)
    {
        var t = _thresholds.TryGetValue(sensorId, out var cfg) ? cfg : default;

        if ((t.LowCritical is double lc && value <= lc) ||
            (t.HighCritical is double hc && value >= hc))
            return DeviceStatus.Critical;

        if ((t.LowWarning is double lw && value <= lw) ||
            (t.HighWarning is double hw && value >= hw))
            return DeviceStatus.Warning;

        return DeviceStatus.Normal;
    }

    private static Dictionary<string, ThresholdConfig> ReadThresholds(IConfiguration config)
    {
        var map = new Dictionary<string, ThresholdConfig>(StringComparer.OrdinalIgnoreCase);

        foreach (var device in config.GetSection("DeviceThresholds").GetChildren())
        {
            map[device.Key] = new ThresholdConfig
            {
                LowCritical = GetDouble(device, "LowCritical"),
                LowWarning = GetDouble(device, "LowWarning"),
                HighWarning = GetDouble(device, "HighWarning"),
                HighCritical = GetDouble(device, "HighCritical"),
            };
        }

        return map;
    }

    private static double? GetDouble(IConfigurationSection section, string key)
        => double.TryParse(section[key], NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : null;
}
