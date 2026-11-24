using SmartShop.Core;
using SmartShop.Models;

namespace SmartShop.Devices;

/// <summary>Reads simulated alarm feedback and applies supervisory overrides.</summary>
public sealed class AlarmController : Controller
{
    public const string DeviceKey = "AlarmController";
    public const int DefaultDeviceId = 12;
    public const string StateActive = "Active";
    public const string StateInactive = "Inactive";
    public const string TelemetryUnit = "bool";

    private readonly LoopedDeviceFile _source;

    public bool IsManualOverride { get; private set; }
    public string? LastReadError { get; private set; }

    // Enables the simulated device rejection scenario in the usability test plan.
    public bool RejectCommands { get; set; }

    public AlarmController(string dataFilePath, int deviceId = DefaultDeviceId)
    {
        _source = new LoopedDeviceFile(dataFilePath);
        DeviceId = deviceId;
        DeviceName = "Alarm";
        Status = DeviceStatus.Warning;
    }

    public override void ReadData()
    {
        if (IsManualOverride)
        {
            return;
        }

        if (!_source.TryRead(DeviceKey, TelemetryUnit, out double value, out string? error)
            || (value != 0 && value != 1))
        {
            IsDataAvailable = false;
            Status = DeviceStatus.Warning;
            LastReadError = error ?? "Invalid AlarmController reading: expected 0 or 1.";
            return;
        }

        ConfirmState(value == 1 ? StateActive : StateInactive);
    }

    public override CommandResult SetConfig(CommandRequest cmd)
    {
        // The command pipeline supplies the identity from the authenticated session.
        string confirmedState = CurrentConfig.State ?? "Unknown";
        if (cmd is null)
        {
            return new CommandResult(false, confirmedState, "No command was received.");
        }

        if (cmd.DeviceId != DeviceId)
        {
            return new CommandResult(false, confirmedState, $"Command was not addressed to alarm {DeviceId}.");
        }

        if (string.IsNullOrWhiteSpace(cmd.AdminUserId))
        {
            return new CommandResult(false, confirmedState, "An authenticated administrator is required.");
        }

        string? state = cmd.RequestedState?.Trim().ToUpperInvariant() switch
        {
            "ACTIVE" => StateActive,
            "INACTIVE" => StateInactive,
            _ => null
        };

        if (state is null)
        {
            return new CommandResult(false, confirmedState, "Choose Active or Inactive for the alarm state.");
        }

        if (RejectCommands)
        {
            return new CommandResult(false, confirmedState, "The simulated alarm rejected the command.");
        }

        ConfirmState(state);
        IsManualOverride = true;
        return new CommandResult(true, state);
    }

    public void ResumeSimulation() => IsManualOverride = false;

    public override TelemetryPayload GetTelemetry()
    {
        double? value = CurrentConfig.State switch
        {
            StateActive => 1,
            StateInactive => 0,
            _ => null
        };

        return new TelemetryPayload(DeviceId, value, TelemetryUnit, Status, IsDataAvailable)
        {
            Timestamp = LastUpdated
        };
    }

    private void ConfirmState(string state)
    {
        CurrentConfig = new DeviceConfig { State = state };
        LastUpdated = DateTime.UtcNow;
        IsDataAvailable = true;
        Status = DeviceStatus.Normal;
        LastReadError = null;
    }
}
