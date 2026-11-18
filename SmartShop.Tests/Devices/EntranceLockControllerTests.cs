using SmartShop.Devices;
using SmartShop.Models;

namespace SmartShop.Tests.Devices
{
    public class EntranceLockControllerTests
    {
        [Fact]
        public void Constructor_UsesSharedControllerContract()
        {
            using var data = new TempDataFile(
                "2026-11-17T12:00:00Z|EntranceLockController|0|bool|Entrance Lock");

            var controller = new EntranceLockController(data.Path);

            Assert.Equal(11, controller.DeviceId);
            Assert.Equal("Entrance Lock", controller.DeviceName);
            Assert.Equal("Unlocked", controller.CurrentConfig.State);
            Assert.False(controller.IsDataAvailable);
            Assert.False(controller.IsManualOverride);
        }

        [Fact]
        public void ReadData_LoopsBaselineFeedback()
        {
            using var data = new TempDataFile(
                "2026-11-17T12:00:00Z|EntranceLockController|0|bool|Entrance Lock",
                "2026-11-17T12:00:01Z|EntranceLockController|1|bool|Entrance Lock");

            var controller = new EntranceLockController(data.Path);

            controller.ReadData();
            Assert.False(controller.IsLocked);
            Assert.True(controller.IsDataAvailable);

            controller.ReadData();
            Assert.True(controller.IsLocked);

            controller.ReadData();
            Assert.False(controller.IsLocked);
        }

        [Fact]
        public void SetConfig_UsesCommandRequestAndCreatesManualOverride()
        {
            using var data = new TempDataFile(
                "2026-11-17T12:00:00Z|EntranceLockController|0|bool|Entrance Lock");

            var controller = new EntranceLockController(data.Path);
            controller.ReadData();

            CommandResult result = controller.SetConfig(
                new CommandRequest(controller.DeviceId, "Locked", "admin-1"));

            Assert.True(result.IsSuccess);
            Assert.Equal("Locked", result.ConfirmedState);
            Assert.True(controller.IsLocked);
            Assert.True(controller.IsManualOverride);

            controller.ReadData();

            Assert.True(controller.IsLocked);
        }

        [Fact]
        public void SetConfig_InvalidState_DoesNotChangeConfirmedState()
        {
            using var data = new TempDataFile(
                "2026-11-17T12:00:00Z|EntranceLockController|0|bool|Entrance Lock");

            var controller = new EntranceLockController(data.Path);
            controller.ReadData();

            CommandResult result = controller.SetConfig(
                new CommandRequest(controller.DeviceId, "Open", "admin-1"));

            Assert.False(result.IsSuccess);
            Assert.Equal("Unlocked", result.ConfirmedState);
            Assert.False(controller.IsLocked);
            Assert.False(controller.IsManualOverride);
        }

        [Fact]
        public void SetConfig_RejectsWrongDeviceAndMissingAdmin()
        {
            using var data = new TempDataFile(
                "2026-11-17T12:00:00Z|EntranceLockController|0|bool|Entrance Lock");

            var controller = new EntranceLockController(data.Path);
            controller.ReadData();

            CommandResult wrongDevice = controller.SetConfig(
                new CommandRequest(999, "Locked", "admin-1"));

            Assert.False(wrongDevice.IsSuccess);
            Assert.False(controller.IsLocked);

            CommandResult missingAdmin = controller.SetConfig(
                new CommandRequest(controller.DeviceId, "Locked", ""));

            Assert.False(missingAdmin.IsSuccess);
            Assert.False(controller.IsLocked);
        }

        [Fact]
        public void ResumeSimulation_AllowsFileFeedbackAfterManualOverride()
        {
            using var data = new TempDataFile(
                "2026-11-17T12:00:00Z|EntranceLockController|0|bool|Entrance Lock",
                "2026-11-17T12:00:01Z|EntranceLockController|0|bool|Entrance Lock");

            var controller = new EntranceLockController(data.Path);
            controller.ReadData();

            controller.SetConfig(
                new CommandRequest(controller.DeviceId, "Locked", "admin-1"));

            Assert.True(controller.IsLocked);

            controller.ResumeSimulation();
            controller.ReadData();

            Assert.False(controller.IsLocked);
            Assert.False(controller.IsManualOverride);
        }

        [Fact]
        public void GetTelemetry_ReturnsSharedPayload()
        {
            using var data = new TempDataFile(
                "2026-11-17T12:00:00Z|EntranceLockController|1|bool|Entrance Lock");

            var controller = new EntranceLockController(data.Path);
            controller.ReadData();

            TelemetryPayload telemetry = controller.GetTelemetry();

            Assert.Equal(controller.DeviceId, telemetry.DeviceId);
            Assert.Equal(1d, telemetry.Value);
            Assert.Equal("bool", telemetry.Unit);
            Assert.Equal(DeviceStatus.Normal, telemetry.Status);
            Assert.True(telemetry.IsDataAvailable);
        }
    }
}
