using SmartShop.Devices;
using SmartShop.Models;

namespace SmartShop.Tests.Devices;

public class AlarmControllerTests
{
    private const string InactiveRecord = "2026-11-20T12:00:00Z|AlarmController|0|bool";
    private const string ActiveRecord = "2026-11-20T12:00:01Z|AlarmController|1|bool";

    [Fact]
    public void ReadData_UsesCurrentUtcTimeAndLoopsAfterTheLastRecord()
    {
        using var data = new TempDataFile(InactiveRecord, ActiveRecord);
        var alarm = new AlarmController(data.Path);

        Assert.Null(alarm.GetTelemetry().Value);

        DateTime beforeRead = DateTime.UtcNow;
        alarm.ReadData();
        DateTime afterRead = DateTime.UtcNow;
        Assert.Equal("Inactive", alarm.GetConfig().State);
        DateTime timestamp = alarm.GetTelemetry().Timestamp;
        Assert.Equal(DateTimeKind.Utc, timestamp.Kind);
        Assert.InRange(timestamp, beforeRead, afterRead);

        alarm.ReadData();
        Assert.Equal("Active", alarm.GetConfig().State);
        Assert.Equal(1d, alarm.GetTelemetry().Value);

        alarm.ReadData();
        Assert.Equal("Inactive", alarm.GetConfig().State);
        Assert.True(alarm.IsDataAvailable);
    }

    [Fact]
    public void ReadData_RetainsLastConfirmedStateWhenFeedbackIsInvalid()
    {
        using var data = new TempDataFile(ActiveRecord, "2026-11-20T12:00:02Z|AlarmController|2|bool");
        var alarm = new AlarmController(data.Path);
        alarm.ReadData();
        DateTime lastValidTimestamp = alarm.LastUpdated;

        alarm.ReadData();

        Assert.False(alarm.IsDataAvailable);
        Assert.Equal("Active", alarm.GetConfig().State);
        Assert.Equal(1d, alarm.GetTelemetry().Value);
        Assert.Equal(lastValidTimestamp, alarm.LastUpdated);
        Assert.NotNull(alarm.LastReadError);
    }

    [Fact]
    public void SetConfig_ReportsConfirmedStateAndOverridesSimulatedFeedbackUntilResumed()
    {
        using var data = new TempDataFile(InactiveRecord, InactiveRecord, ActiveRecord);
        var alarm = new AlarmController(data.Path);
        alarm.ReadData();

        CommandResult activate = alarm.SetConfig(new CommandRequest(alarm.DeviceId, " active ", "admin-1"));
        Assert.True(activate.IsSuccess);
        Assert.Equal("Active", activate.ConfirmedState);

        alarm.ReadData();
        Assert.Equal("Active", alarm.GetConfig().State);

        CommandResult deactivate = alarm.SetConfig(new CommandRequest(alarm.DeviceId, "Inactive", "admin-1"));
        Assert.True(deactivate.IsSuccess);
        Assert.Equal("Inactive", deactivate.ConfirmedState);

        alarm.ResumeSimulation();
        alarm.ReadData();
        alarm.ReadData();
        Assert.Equal("Active", alarm.GetConfig().State);
    }

    [Theory]
    [InlineData(999, "Inactive", "admin-1", false)]
    [InlineData(12, "Inactive", "", false)]
    [InlineData(12, "Unsupported", "admin-1", false)]
    [InlineData(12, "Inactive", "admin-1", true)]
    public void SetConfig_RejectedCommandsKeepTheLastConfirmedState(
        int deviceId, string requestedState, string admin, bool rejectCommands)
    {
        using var data = new TempDataFile(ActiveRecord);
        var alarm = new AlarmController(data.Path) { RejectCommands = rejectCommands };
        alarm.ReadData();
        DateTime lastValidTimestamp = alarm.LastUpdated;

        CommandResult result = alarm.SetConfig(new CommandRequest(deviceId, requestedState, admin));

        Assert.False(result.IsSuccess);
        Assert.Equal("Active", result.ConfirmedState);
        Assert.NotNull(result.ErrorMessage);
        Assert.Equal("Active", alarm.GetConfig().State);
        Assert.Equal(lastValidTimestamp, alarm.LastUpdated);
        Assert.False(alarm.IsManualOverride);
    }
}
