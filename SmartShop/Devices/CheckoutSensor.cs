using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SmartShop.Core;
using SmartShop.Models;

namespace SmartShop.Devices
{
    /// <summary>
    /// Monitors self-checkout lane activity (REQ-TEL-006): the number of customers
    /// currently being served or waiting at the checkout lanes.
    /// Reads a looped ASCII simulation file (REQ-SYS-010, REQ-TEL-001) and classifies
    /// each reading against configurable thresholds (REQ-STS-001, REQ-STS-002).
    /// </summary>
    /// <remarks>
    /// File format (docs/simulation-file-format.md, one record per line):
    /// <c>timestamp|CheckoutSensor|value|count|label</c>.
    /// Blank lines and lines starting with '#' are skipped.
    /// </remarks>
    public class CheckoutSensor : Sensor
    {
        /// <summary>Sensor key expected in the second field of every simulation record.</summary>
        public const string SensorKey = "CheckoutSensor";

        /// <summary>Proposed device ID (3rd sensor in the SRS device list). Confirm with the team.</summary>
        public const int DefaultDeviceId = 3;

        public const string DefaultUnit = "count";
        public const double DefaultHighWarning = 6;
        public const double DefaultHighCritical = 9;

        /// <summary>Readings above this are treated as invalid records.</summary>
        public const double MaxPlausibleCount = 100;

        private readonly string _dataFilePath;
        private int _nextRecordIndex;
        private double? _candidateValue;

        /// <summary>Number of simulated kiosk purchases recorded since start-up (REQ-KSK-002).</summary>
        public int TotalTransactions { get; private set; }

        /// <summary>Why the last read failed, for the EventLog (REQ-LOG-001). Null after a good read.</summary>
        public string? LastReadError { get; private set; }

        public CheckoutSensor(string dataFilePath, int deviceId = DefaultDeviceId)
        {
            _dataFilePath = dataFilePath ?? throw new ArgumentNullException(nameof(dataFilePath));
            DeviceId = deviceId;
            DeviceName = "Checkout Activity";
            Unit = DefaultUnit;
            Thresholds = new ThresholdConfig
            {
                HighWarning = DefaultHighWarning,
                HighCritical = DefaultHighCritical
            };
            IsDataAvailable = false;
        }

        /// <summary>
        /// Reads the next record of the simulation file, restarting at the first record
        /// after the last one. A missing file or invalid record marks the data unavailable
        /// and keeps the last known value (REQ-TEL-003, REQ-NFR-REL-001). Never throws.
        /// </summary>
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
            _nextRecordIndex = index + 1; // wraps to 0 on the next call after the last record
            string record = records[index];

            _candidateValue = TryParseRecord(record);
            if (!Validate())
            {
                MarkUnavailable($"Invalid checkout record: '{record}'");
                return;
            }

            Value = _candidateValue;
            LastReadError = null;
            LastUpdated = DateTime.UtcNow;
            ClassifyStatus();
        }

        /// <summary>
        /// Checks the record that was just parsed: it must be a whole, non-negative
        /// customer count no larger than <see cref="MaxPlausibleCount"/>.
        /// Sets <see cref="Device.IsDataAvailable"/> to the result.
        /// </summary>
        public override bool Validate()
        {
            bool isValid = _candidateValue is double v
                           && double.IsFinite(v)
                           && v >= 0
                           && v <= MaxPlausibleCount
                           && v == Math.Floor(v);
            IsDataAvailable = isValid;
            return isValid;
        }

        /// <summary>
        /// Records a simulated kiosk purchase (REQ-KSK-002): one more active checkout
        /// transaction until the next simulation reading replaces the value.
        /// </summary>
        /// <returns>The updated telemetry to publish to the dashboard.</returns>
        public TelemetryPayload RecordTransaction(DateTime? timestampUtc = null)
        {
            TotalTransactions++;
            Value = (Value ?? 0) + 1;
            IsDataAvailable = true;
            LastReadError = null;
            LastUpdated = timestampUtc ?? DateTime.UtcNow;
            ClassifyStatus();
            return GetTelemetry();
        }

        /// <summary>Publishes the shared telemetry structure (REQ-INT-TEL-001).</summary>
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

        /// <summary>Returns the reading in a record, or null if the record is malformed.</summary>
        private static double? TryParseRecord(string record)
        {
            string[] fields = record.Split('|');
            if (fields.Length < 4)
            {
                return null;
            }

            bool hasTimestamp = DateTime.TryParse(
                fields[0].Trim(),
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out _);
            bool isThisSensor = string.Equals(fields[1].Trim(), SensorKey, StringComparison.OrdinalIgnoreCase);
            bool hasValue = double.TryParse(
                fields[2].Trim(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double value);

            return hasTimestamp && isThisSensor && hasValue ? value : null;
        }

        private void MarkUnavailable(string reason)
        {
            // Value is deliberately left alone: it is the "last known state" (REQ-TEL-003).
            IsDataAvailable = false;
            LastReadError = reason;
            LastUpdated = DateTime.UtcNow;
            ClassifyStatus();
        }
    }
}
