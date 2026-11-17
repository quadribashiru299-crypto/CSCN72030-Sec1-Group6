using SmartShop.Devices;
using SmartShop.Models;

namespace SmartShop.Tests.Devices
{
    public class CheckoutStationControllerTests
    {
        private const string Admin = "admin-01";

        private static string Line(int value, string device = CheckoutStationController.DeviceKey) =>
            $"2026-11-16T09:00:00Z|{device}|{value}|bool|Checkout Lane 1";

        private static CommandRequest Command(string state, string adminUserId = Admin,
                                              int deviceId = CheckoutStationController.DefaultDeviceId) =>
            new CommandRequest(deviceId, state, adminUserId);

        [Fact]
        public void NewController_StartsOpenAndAvailable()
        {
            var controller = new CheckoutStationController();

            Assert.Equal(CheckoutStationController.StateOpen, controller.GetConfig().State);
            Assert.True(controller.IsDataAvailable);
            Assert.Equal(1, controller.GetTelemetry().Value);
        }

        [Fact]
        public void SetConfig_Closed_ByAdmin_SucceedsAndConfirmsState()
        {
            var controller = new CheckoutStationController();

            CommandResult result = controller.SetConfig(Command("Closed"));

            Assert.True(result.IsSuccess);
            Assert.Equal(CheckoutStationController.StateClosed, result.ConfirmedState);
            Assert.Null(result.ErrorMessage);
            Assert.Equal(CheckoutStationController.StateClosed, controller.GetConfig().State);
            Assert.Equal(0, controller.GetTelemetry().Value);
            Assert.Equal(CheckoutStationController.StateOpen, controller.LastPreviousState);
        }

        [Theory]
        [InlineData("open")]
        [InlineData(" OPEN ")]
        [InlineData("Open")]
        public void SetConfig_AcceptsAnyLetterCase(string requested)
        {
            var controller = new CheckoutStationController();
            controller.SetConfig(Command("Closed"));

            CommandResult result = controller.SetConfig(Command(requested));

            Assert.True(result.IsSuccess);
            Assert.Equal(CheckoutStationController.StateOpen, result.ConfirmedState);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void SetConfig_WithoutAdministrator_IsRejectedAndStateUnchanged(string adminUserId)
        {
            var controller = new CheckoutStationController();

            CommandResult result = controller.SetConfig(Command("Closed", adminUserId));

            Assert.False(result.IsSuccess);
            Assert.Equal(CheckoutStationController.StateOpen, result.ConfirmedState);
            Assert.NotNull(result.ErrorMessage);
            Assert.Equal(CheckoutStationController.StateOpen, controller.GetConfig().State);
        }

        [Fact]
        public void SetConfig_NullAdministrator_IsRejected()
        {
            var controller = new CheckoutStationController();

            CommandResult result = controller.SetConfig(Command("Closed", null!));

            Assert.False(result.IsSuccess);
            Assert.Equal(CheckoutStationController.StateOpen, controller.GetConfig().State);
        }

        [Theory]
        [InlineData("Locked")]
        [InlineData("1")]
        [InlineData("")]
        public void SetConfig_UnsupportedState_IsRejectedAndStateUnchanged(string requested)
        {
            var controller = new CheckoutStationController();

            CommandResult result = controller.SetConfig(Command(requested));

            Assert.False(result.IsSuccess);
            Assert.Equal(CheckoutStationController.StateOpen, result.ConfirmedState);
            Assert.Equal(CheckoutStationController.StateOpen, controller.GetConfig().State);
        }

        [Fact]
        public void SetConfig_WrongDeviceId_IsRejected()
        {
            var controller = new CheckoutStationController();

            CommandResult result = controller.SetConfig(Command("Closed", deviceId: 99));

            Assert.False(result.IsSuccess);
            Assert.Equal(CheckoutStationController.StateOpen, controller.GetConfig().State);
        }

        [Fact]
        public void SetConfig_WhenSimulatorFails_ReportsFailureAndDoesNotClaimChange()
        {
            var controller = new CheckoutStationController { SimulateCommandFailure = true };

            CommandResult result = controller.SetConfig(Command("Closed"));

            Assert.False(result.IsSuccess);
            Assert.Equal(CheckoutStationController.StateOpen, result.ConfirmedState);
            Assert.NotNull(result.ErrorMessage);
            Assert.Equal(CheckoutStationController.StateOpen, controller.GetConfig().State);
            Assert.False(controller.IsManualOverride);
        }

        [Fact]
        public void SetConfig_Null_IsRejected()
        {
            var controller = new CheckoutStationController();

            CommandResult result = controller.SetConfig(null!);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public void ReadData_FollowsSimulationFileAndLoops()
        {
            using var file = new TempDataFile(Line(1), Line(0));
            var controller = new CheckoutStationController(file.Path);

            var states = new List<string>();
            for (int i = 0; i < 3; i++)
            {
                controller.ReadData();
                states.Add(controller.GetConfig().State);
            }

            Assert.Equal(new[] { "Open", "Closed", "Open" }, states);
        }

        [Fact]
        public void ReadData_AfterAdminCommand_DoesNotOverrideAdminChoice()
        {
            using var file = new TempDataFile(Line(1));
            var controller = new CheckoutStationController(file.Path);
            controller.SetConfig(Command("Closed"));

            controller.ReadData();

            Assert.True(controller.IsManualOverride);
            Assert.Equal(CheckoutStationController.StateClosed, controller.GetConfig().State);
        }

        [Fact]
        public void ResumeSimulation_LetsFileDriveStateAgain()
        {
            using var file = new TempDataFile(Line(1));
            var controller = new CheckoutStationController(file.Path);
            controller.SetConfig(Command("Closed"));

            controller.ResumeSimulation();
            controller.ReadData();

            Assert.False(controller.IsManualOverride);
            Assert.Equal(CheckoutStationController.StateOpen, controller.GetConfig().State);
        }

        [Fact]
        public void ReadData_MissingFile_MarksUnavailableButKeepsLastStateAndAcceptsCommands()
        {
            var controller = new CheckoutStationController(TempDataFile.MissingPath());

            Exception? error = Record.Exception(() => controller.ReadData());

            Assert.Null(error);
            Assert.False(controller.IsDataAvailable);
            Assert.Equal(DeviceStatus.Warning, controller.Status);
            Assert.Equal(CheckoutStationController.StateOpen, controller.GetConfig().State);
            Assert.NotNull(controller.LastReadError);

            CommandResult result = controller.SetConfig(Command("Closed"));
            Assert.True(result.IsSuccess);
        }

        [Theory]
        [InlineData("2026-11-16T09:00:00Z|CheckoutStationController|2|bool|Lane")]
        [InlineData("2026-11-16T09:00:00Z|AlarmController|1|bool|Alarm")]
        [InlineData("garbage")]
        public void ReadData_InvalidRecord_MarksUnavailableAndKeepsLastState(string badRecord)
        {
            using var file = new TempDataFile(Line(0), badRecord);
            var controller = new CheckoutStationController(file.Path);

            controller.ReadData(); // Closed
            controller.ReadData(); // invalid

            Assert.False(controller.IsDataAvailable);
            Assert.Equal(CheckoutStationController.StateClosed, controller.GetConfig().State);
        }

        [Fact]
        public void GetTelemetry_UsesSharedStructure()
        {
            var controller = new CheckoutStationController(deviceId: 77);

            TelemetryPayload telemetry = controller.GetTelemetry();

            Assert.Equal(77, telemetry.DeviceId);
            Assert.Equal(CheckoutStationController.TelemetryUnit, telemetry.Unit);
            Assert.Equal(DeviceStatus.Normal, telemetry.Status);
            Assert.True(telemetry.IsDataAvailable);
        }
    }
}
