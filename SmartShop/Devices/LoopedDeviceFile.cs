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
        out double value,
        [NotNullWhen(false)] out string? error)
    {
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
        if (fields.Length < 3
            || !string.Equals(fields[0].Trim(), deviceKey, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(fields[2].Trim(), unit, StringComparison.OrdinalIgnoreCase)
            || !double.TryParse(fields[1], NumberStyles.Float, CultureInfo.InvariantCulture, out value)
            || !double.IsFinite(value))
        {
            error = $"Invalid {deviceKey} record: '{record}'.";
            return false;
        }

        error = null;
        return true;
    }
}
