using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace SmartShop.Devices;

internal sealed class LoopedDeviceFile(string path)
{
    private readonly string _path = path ?? throw new ArgumentNullException(nameof(path));
    private int _nextRecordIndex;

    public bool TryRead(
        string deviceKey,
        string unit,
        out DateTime timestamp,
        out double value,
        [NotNullWhen(false)] out string? error)
    {
        timestamp = default;
        value = default;
        string[] records;

        try
        {
            records = File.ReadLines(_path)
                .Select(line => line.Trim())
                .Where(line => line.Length > 0 && !line.StartsWith('#'))
                .ToArray();
        }
        catch (Exception ex) when (ex is IOException
                                   or UnauthorizedAccessException
                                   or ArgumentException
                                   or NotSupportedException)
        {
            error = $"Cannot read simulation file '{_path}': {ex.Message}";
            return false;
        }

        if (records.Length == 0)
        {
            error = $"Simulation file '{_path}' has no records.";
            return false;
        }

        int index = _nextRecordIndex % records.Length;
        _nextRecordIndex = index + 1;

        string record = records[index];
        string[] fields = record.Split('|');
        if (fields.Length < 4
            || !string.Equals(fields[1].Trim(), deviceKey, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(fields[3].Trim(), unit, StringComparison.OrdinalIgnoreCase)
            || !DateTime.TryParse(fields[0], CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out timestamp)
            || !double.TryParse(fields[2], NumberStyles.Float, CultureInfo.InvariantCulture, out value)
            || !double.IsFinite(value))
        {
            error = $"Invalid {deviceKey} record: '{record}'.";
            return false;
        }

        error = null;
        return true;
    }
}
