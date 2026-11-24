using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SmartShop.Core;
using SmartShop.Models;

namespace SmartShop.Devices
{
    /// <summary>
    /// Simulated entrance-lock actuator.
    /// Baseline feedback comes from its looped ASCII file until an administrator
    /// command establishes a manual override.
    /// </summary>
    public sealed class EntranceLockController : Controller
    {
        public const string DeviceKey = "EntranceLockController";
        public const int DefaultDeviceId = 11;
        public const string StateLocked = "Locked";
        public const string StateUnlocked = "Unlocked";
        public const string TelemetryUnit = "bool";

        private readonly string _dataFilePath;
        private int _nextRecordIndex;

        public bool IsLocked => CurrentConfig.State == StateLocked;
        public bool IsManualOverride { get; private set; }
        public string? LastPreviousState { get; private set; }
        public string? LastReadError { get; private set; }

        public EntranceLockController(string dataFilePath, int deviceId = DefaultDeviceId)
        {
            _dataFilePath = dataFilePath ?? throw new ArgumentNullException(nameof(dataFilePath));

            DeviceId = deviceId;
            DeviceName = "Entrance Lock";
            CurrentConfig = new DeviceConfig
            {
                State = StateUnlocked,
                Value = null
            };

            IsDataAvailable = false;
            Status = DeviceStatus.Warning;
        }

        public override void ReadData()
        {
            if (IsManualOverride)
            {
                return;
            }

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

            string? state = TryParseRecord(record);

            if (state is null)
            {
                MarkUnavailable($"Invalid entrance lock record: '{record}'");
                return;
            }

            CurrentConfig = new DeviceConfig
            {
                State = state,
                Value = null
            };

            IsDataAvailable = true;
            Status = DeviceStatus.Normal;
            LastReadError = null;
            LastUpdated = DateTime.UtcNow;
        }

        public override CommandResult SetConfig(CommandRequest cmd)
        {
            string currentState = CurrentConfig.State;

            if (cmd is null)
            {
                return new CommandResult(false, currentState, "No command was received.");
            }

            LastPreviousState = currentState;

            if (cmd.DeviceId != DeviceId)
            {
                return new CommandResult(
                    false,
                    currentState,
                    $"Command was addressed to device {cmd.DeviceId}, not the entrance lock ({DeviceId}).");
            }

            if (string.IsNullOrWhiteSpace(cmd.AdminUserId))
            {
                return new CommandResult(
                    false,
                    currentState,
                    "Command rejected: an authenticated administrator is required.");
            }

            string? requestedState = NormalizeState(cmd.RequestedState);

            if (requestedState is null)
            {
                return new CommandResult(
                    false,
                    currentState,
                    $"Unsupported state '{cmd.RequestedState}'. Use '{StateLocked}' or '{StateUnlocked}'.");
            }

            CurrentConfig = new DeviceConfig
            {
                State = requestedState,
                Value = null
            };

            IsManualOverride = true;
            IsDataAvailable = true;
            Status = DeviceStatus.Normal;
            LastReadError = null;
            LastUpdated = DateTime.UtcNow;

            return new CommandResult(true, requestedState);
        }

        public void ResumeSimulation()
        {
            IsManualOverride = false;
        }

        public override TelemetryPayload GetTelemetry()
        {
            double value = IsLocked ? 1 : 0;

            return new TelemetryPayload(DeviceId, value, TelemetryUnit, Status, IsDataAvailable)
            {
                Timestamp = LastUpdated == default ? DateTime.UtcNow : LastUpdated
            };
        }

        public static string? NormalizeState(string? requestedState)
        {
            string trimmed = requestedState?.Trim() ?? string.Empty;

            if (string.Equals(trimmed, StateLocked, StringComparison.OrdinalIgnoreCase))
            {
                return StateLocked;
            }

            if (string.Equals(trimmed, StateUnlocked, StringComparison.OrdinalIgnoreCase))
            {
                return StateUnlocked;
            }

            return null;
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

        private static string? TryParseRecord(string record)
        {
            string[] fields = record.Split('|');

            if (fields.Length < 3)
            {
                return null;
            }

            bool isEntranceLock = string.Equals(
                fields[0].Trim(),
                DeviceKey,
                StringComparison.OrdinalIgnoreCase);

            bool hasValue = double.TryParse(
                fields[1].Trim(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double value);

            if (!isEntranceLock || !hasValue)
            {
                return null;
            }

            if (value == 1)
            {
                return StateLocked;
            }

            if (value == 0)
            {
                return StateUnlocked;
            }

            return null;
        }

        private void MarkUnavailable(string reason)
        {
            // Keep the previous confirmed state as the last known state.
            IsDataAvailable = false;
            Status = DeviceStatus.Warning;
            LastReadError = reason;
            LastUpdated = DateTime.UtcNow;
        }
    }
}
