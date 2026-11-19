using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SmartShop.Models;

namespace SmartShop.Core
{
    public struct ThresholdConfig
    {
        public double? LowCritical;
        public double? LowWarning;
        public double? HighWarning;
        public double? HighCritical;
    }

    public abstract class Sensor : Device
    {
        private List<string>? _simulationRecords;
        private int _nextSimulationIndex;

        public double? Value { get; set; }
        public string Unit { get; set; }
        public ThresholdConfig Thresholds { get; set; }

        /// <summary>
        /// Looped ASCII simulation file this sensor reads from. Set it in the
        /// concrete constructor; when it is null the sensor stays unavailable.
        /// </summary>
        protected string? SimulationFilePath { get; set; }

        /// <summary>
        /// Second field of the simulation record that identifies this sensor.
        /// Defaults to the concrete class name (e.g. ShelfStockSensor).
        /// </summary>
        protected virtual string SimulationKey => GetType().Name;

        /// <summary>
        /// Value parsed from the current record, before validation. Concrete
        /// sensors read this from <see cref="Validate"/>.
        /// </summary>
        protected double? CandidateValue { get; private set; }

        /// <summary>
        /// Why the last read produced no usable value (missing file, unparsable
        /// record). Null after a successful read.
        /// </summary>
        public string? LastSimulationError { get; private set; }

        //Checks if the received data makes sense and sets the IsDataAvailable attribute
        public abstract bool Validate();

        //Compares the read value with configured and returns resulting status
        public DeviceStatus ClassifyStatus()
        {
            if (!IsDataAvailable || Value == null)
            {
                Status = DeviceStatus.Warning;
                return DeviceStatus.Warning;
            }
            if((Thresholds.LowCritical != null && Value <= Thresholds.LowCritical) || (Thresholds.HighCritical != null && Value >= Thresholds.HighCritical))
            {
                Status = DeviceStatus.Critical;
                return DeviceStatus.Critical;
            }
            if((Thresholds.LowWarning != null && Value <= Thresholds.LowWarning) || (Thresholds.HighWarning != null && Value >= Thresholds.HighWarning))
            {
                Status = DeviceStatus.Warning;
                return DeviceStatus.Warning;
            }
            Status = DeviceStatus.Normal;
            return DeviceStatus.Normal;
        }

        /// <summary>
        /// Reads exactly one record from the looped ASCII simulation file and
        /// refreshes Value / Status (REQ-TEL-001). The cursor wraps around, so the
        /// stream never ends. A missing or invalid file keeps the last known value
        /// and marks the sensor unavailable instead of throwing (REQ-NFR-REL-001).
        /// </summary>
        public override void ReadData()
        {
            if (SimulationFilePath is null)
            {
                MarkUnavailable("No simulation file configured for this sensor.");
                return;
            }

            string? record = NextSimulationRecord();

            if (record is null)
            {
                return;
            }

            CandidateValue = ParseValue(record);

            if (CandidateValue is null || !Validate())
            {
                MarkUnavailable($"Invalid record for {SimulationKey}: '{record}'");
                return;
            }

            Value = CandidateValue;
            IsDataAvailable = true;
            LastSimulationError = null;
            LastUpdated = DateTime.UtcNow;
            ClassifyStatus();
        }

        /// <summary>
        /// Every sensor knows how to build its own telemetry: when a simulation
        /// file is configured the payload always carries the next reading, so no
        /// external mock-data service is needed.
        /// </summary>
        public override TelemetryPayload GetTelemetry()
        {
            if (SimulationFilePath is not null)
            {
                ReadData();
            }

            return new TelemetryPayload(DeviceId, Value, Unit, Status, IsDataAvailable)
            {
                Timestamp = LastUpdated == default ? DateTime.UtcNow : LastUpdated
            };
        }

        /// <summary>
        /// Returns the next non-comment, non-blank record belonging to this
        /// sensor, or null when the file cannot be read or holds no record.
        /// </summary>
        private string? NextSimulationRecord()
        {
            if (_simulationRecords is null)
            {
                try
                {
                    _simulationRecords = LoadSimulationRecords();
                }
                catch (Exception ex) when (ex is IOException
                                           || ex is UnauthorizedAccessException
                                           || ex is ArgumentException
                                           || ex is NotSupportedException)
                {
                    MarkUnavailable($"Cannot read simulation file '{SimulationFilePath}': {ex.Message}");
                    return null;
                }
            }

            if (_simulationRecords.Count == 0)
            {
                MarkUnavailable($"Simulation file '{SimulationFilePath}' has no records for {SimulationKey}.");
                return null;
            }

            int index = _nextSimulationIndex % _simulationRecords.Count;
            _nextSimulationIndex = (index + 1) % _simulationRecords.Count;
            return _simulationRecords[index];
        }

        private List<string> LoadSimulationRecords()
        {
            var records = new List<string>();

            foreach (string rawLine in File.ReadAllLines(SimulationFilePath!))
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

        /// <summary>
        /// Splits "timestamp|key|value|unit|label" and returns the numeric value
        /// only when the timestamp parses, the key matches this sensor and the
        /// value is numeric.
        /// </summary>
        private double? ParseValue(string record)
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

            bool isThisSensor = string.Equals(
                fields[1].Trim(),
                SimulationKey,
                StringComparison.OrdinalIgnoreCase);

            if (!hasTimestamp || !isThisSensor)
            {
                return null;
            }

            return double.TryParse(
                fields[2].Trim(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double value)
                ? value
                : null;
        }

        /// <summary>
        /// Keeps the last known value for the HMI and flags the reading stale.
        /// </summary>
        private void MarkUnavailable(string reason)
        {
            IsDataAvailable = false;
            LastSimulationError = reason;
            LastUpdated = DateTime.UtcNow;
            ClassifyStatus();
        }
    }
}
