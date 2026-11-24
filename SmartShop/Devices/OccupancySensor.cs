using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SmartShop.Core;
using SmartShop.Models;

namespace SmartShop.Devices
{
    /// <summary>
    /// Monitors the number of people currently in the store.
    /// Reads a looped ASCII simulation file and publishes the shared telemetry contract.
    /// </summary>
    public sealed class OccupancySensor : Sensor
    {
        public const string SensorKey = "OccupancySensor";
        public const int DefaultDeviceId = 5;
        public const string DefaultUnit = "count";
        public const double DefaultHighWarning = 15;
        public const double DefaultHighCritical = 20;

        private readonly string _dataFilePath;
        private int _nextRecordIndex;
        private double? _candidateValue;

        public int CurrentOccupancy => Value is double value ? (int)value : 0;
        public string? LastReadError { get; private set; }

        public OccupancySensor(string dataFilePath, int deviceId = DefaultDeviceId)
        {
            _dataFilePath = dataFilePath ?? throw new ArgumentNullException(nameof(dataFilePath));

            DeviceId = deviceId;
            DeviceName = "Store Occupancy";
            Unit = DefaultUnit;
            Thresholds = new ThresholdConfig
            {
                HighWarning = DefaultHighWarning,
                HighCritical = DefaultHighCritical
            };

            IsDataAvailable = false;
            Status = DeviceStatus.Warning;
        }

        public override void ReadData()
        {
            List<string> records;

            try
            {
                records = LoadRecords();
            }
            catch (Exception ex) when (ex is IOException
                                       || ex is UnauthorizedAccessException
                                       || ex is ArgumentException
                                       || ex is NotSupportedException)
            {
                MarkUnavailable($"Cannot read simulation file '{_dataFilePath}': {ex.Message}");
                return;
            }

            if (records.Count == 0)
            {
                MarkUnavailable($"Simulation file '{_dataFilePath}' has no records.");
                return;
            }

            int index = _nextRecordIndex % records.Count;
            _nextRecordIndex = index + 1;
            string record = records[index];

            _candidateValue = TryParseRecord(record);

            if (!Validate())
            {
                MarkUnavailable($"Invalid occupancy record: '{record}'");
                return;
            }

            Value = _candidateValue;
            IsDataAvailable = true;
            LastReadError = null;
            LastUpdated = DateTime.UtcNow;
            ClassifyStatus();
        }

        public override bool Validate()
        {
            bool isValid = _candidateValue is double value
                           && double.IsFinite(value)
                           && value >= 0
                           && value <= int.MaxValue
                           && value == Math.Floor(value);

            IsDataAvailable = isValid;
            return isValid;
        }

        public override TelemetryPayload GetTelemetry()
        {
            return new TelemetryPayload(DeviceId, Value, Unit, Status, IsDataAvailable)
            {
                Timestamp = LastUpdated == default ? DateTime.UtcNow : LastUpdated
            };
        }

        private List<string> LoadRecords()
        {
            var records = new List<string>();

            foreach (string rawLine in File.ReadAllLines(_dataFilePath))
            {
                string line = rawLine.Trim();

                if (line.Length == 0 || line.StartsWith('#'))
                {
                    continue;
                }

                records.Add(line);
            }

            return records;
        }

        private static double? TryParseRecord(string record)
        {
            string[] fields = record.Split('|');

            if (fields.Length < 3)
            {
                return null;
            }

            bool isOccupancySensor = string.Equals(
                fields[0].Trim(),
                SensorKey,
                StringComparison.OrdinalIgnoreCase);

            bool hasValue = double.TryParse(
                fields[1].Trim(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double value);

            string unit = fields[2].Trim();
            bool validUnit = string.Equals(unit, "count", StringComparison.OrdinalIgnoreCase)
                             || string.Equals(unit, "people", StringComparison.OrdinalIgnoreCase);

            return isOccupancySensor && hasValue && validUnit
                ? value
                : null;
        }

        private void MarkUnavailable(string reason)
        {
            // Keep Value unchanged so the HMI can display the last known reading.
            IsDataAvailable = false;
            LastReadError = reason;
            LastUpdated = DateTime.UtcNow;
            ClassifyStatus();
        }
    }
}
