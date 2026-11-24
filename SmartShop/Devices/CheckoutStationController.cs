using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SmartShop.Core;
using SmartShop.Models;

namespace SmartShop.Devices
{
    /// <summary>
    /// Opens or closes a self-checkout lane on administrator command (REQ-CTL-005).
    /// The lane's simulated field state comes from a looped ASCII file (REQ-SYS-010);
    /// once an administrator issues a command, that manual override wins until
    /// <see cref="ResumeSimulation"/> is called.
    /// The displayed state only changes after a confirmed result (REQ-CTL-007).
    /// </summary>
    /// <remarks>
    /// File format: <c>timestamp|CheckoutStationController|value|bool|label</c>,
    /// where value 1 = Open and 0 = Closed. Blank lines and '#' lines are skipped.
    /// RBAC is enforced by the MVC layer (REQ-AUTH-001); this class also refuses any
    /// command without an administrator user ID as a second line of defence.
    /// </remarks>
    public class CheckoutStationController : Controller
    {
        public const string DeviceKey = "CheckoutStationController";

        /// <summary>Proposed device ID (3rd controller in the SRS list, after the 7 sensors). Confirm with the team.</summary>
        public const int DefaultDeviceId = 10;

        public const string StateOpen = "Open";
        public const string StateClosed = "Closed";
        public const string TelemetryUnit = "bool";

        private readonly string? _dataFilePath;
        private int _nextRecordIndex;

        /// <summary>True after an administrator command; the simulation file no longer changes the state.</summary>
        public bool IsManualOverride { get; private set; }

        /// <summary>When true, every command fails without changing state (for testing REQ-CTL-007).</summary>
        public bool SimulateCommandFailure { get; set; }

        /// <summary>State before the most recent command, for the AuditTrail (REQ-LOG-002).</summary>
        public string? LastPreviousState { get; private set; }

        /// <summary>Why the last simulation read failed, for the EventLog (REQ-LOG-001). Null after a good read.</summary>
        public string? LastReadError { get; private set; }

        /// <param name="dataFilePath">Looped simulation file, or null to start Open with no simulated changes.</param>
        /// <param name="deviceId">Device ID used by the HMI and logs.</param>
        public CheckoutStationController(string? dataFilePath = null, int deviceId = DefaultDeviceId)
        {
            _dataFilePath = dataFilePath;
            DeviceId = deviceId;
            DeviceName = "Checkout Station";
            CurrentConfig = new DeviceConfig { State = StateOpen, Value = null };
            IsDataAvailable = true;
            Status = DeviceStatus.Normal;
            LastUpdated = DateTime.UtcNow;
        }

        /// <summary>True when the lane is open (class diagram: IsLaneOpen).</summary>
        public bool IsLaneOpen => CurrentConfig.State == StateOpen;

        /// <summary>
        /// Reads the next simulated lane state, restarting at the first record after the last.
        /// Ignored while an administrator override is active. Never throws.
        /// </summary>
        public override void ReadData()
        {
            if (_dataFilePath is null || IsManualOverride)
            {
                return;
            }

            List<string> records;
            try
            {
                records = LoadRecords(_dataFilePath);
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
                MarkUnavailable($"Invalid checkout station record: '{record}'");
                return;
            }

            CurrentConfig = new DeviceConfig { State = state, Value = null };
            IsDataAvailable = true;
            Status = DeviceStatus.Normal;
            LastReadError = null;
            LastUpdated = DateTime.UtcNow;
        }

        /// <summary>
        /// Applies an Open/Closed command (REQ-CTL-005, REQ-INT-CTL-001) and returns the
        /// confirmed state. On any failure the lane state is left unchanged.
        /// </summary>
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
                return new CommandResult(false, currentState,
                    $"Command was addressed to device {cmd.DeviceId}, not the checkout station ({DeviceId}).");
            }

            if (string.IsNullOrWhiteSpace(cmd.AdminUserId))
            {
                return new CommandResult(false, currentState,
                    "Command rejected: an authenticated administrator is required.");
            }

            string? requestedState = NormalizeState(cmd.RequestedState);
            if (requestedState is null)
            {
                return new CommandResult(false, currentState,
                    $"Unsupported state '{cmd.RequestedState}'. Use '{StateOpen}' or '{StateClosed}'.");
            }

            if (SimulateCommandFailure)
            {
                return new CommandResult(false, currentState,
                    "The checkout station did not confirm the change. The lane state is unchanged.");
            }

            CurrentConfig = new DeviceConfig { State = requestedState, Value = null };
            IsManualOverride = true;
            IsDataAvailable = true;
            Status = DeviceStatus.Normal;
            LastUpdated = DateTime.UtcNow;

            return new CommandResult(true, requestedState);
        }

        /// <summary>Clears the administrator override so the simulation file drives the lane again.</summary>
        public void ResumeSimulation()
        {
            IsManualOverride = false;
        }

        /// <summary>Publishes the shared telemetry structure (REQ-INT-TEL-001): 1 = Open, 0 = Closed.</summary>
        public override TelemetryPayload GetTelemetry()
        {
            double value = IsLaneOpen ? 1 : 0;
            return new TelemetryPayload(DeviceId, value, TelemetryUnit, Status, IsDataAvailable)
            {
                Timestamp = LastUpdated
            };
        }

        /// <summary>Maps "open"/"closed" in any letter case to the canonical state, or null.</summary>
        public static string? NormalizeState(string? requested)
        {
            string trimmed = requested?.Trim() ?? string.Empty;
            if (string.Equals(trimmed, StateOpen, StringComparison.OrdinalIgnoreCase))
            {
                return StateOpen;
            }
            if (string.Equals(trimmed, StateClosed, StringComparison.OrdinalIgnoreCase))
            {
                return StateClosed;
            }
            return null;
        }

        private static List<string> LoadRecords(string path)
        {
            var records = new List<string>();
            foreach (string rawLine in File.ReadAllLines(path))
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

        /// <summary>Returns "Open"/"Closed" for a valid record, or null if it is malformed.</summary>
        private static string? TryParseRecord(string record)
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
            bool isThisDevice = string.Equals(fields[1].Trim(), DeviceKey, StringComparison.OrdinalIgnoreCase);
            bool hasValue = double.TryParse(
                fields[2].Trim(),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double value);

            if (!hasTimestamp || !isThisDevice || !hasValue)
            {
                return null;
            }
            if (value == 1)
            {
                return StateOpen;
            }
            if (value == 0)
            {
                return StateClosed;
            }
            return null;
        }

        private void MarkUnavailable(string reason)
        {
            // The last confirmed state is kept and shown as "last known state" (REQ-TEL-003).
            IsDataAvailable = false;
            Status = DeviceStatus.Warning;
            LastReadError = reason;
            LastUpdated = DateTime.UtcNow;
        }
    }
}
