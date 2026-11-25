using SmartShop.Devices;
using SmartShop.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartShop.Tests.Devices
{
    public class VentilationControllerTests
    {
        [Fact]
        public void Constructor_ValidParameters_SetsInitialModeAndFanSpeed()
        {
            //arrange
            //act
            var controller = new VentilationController(20, "Store HVAC", "Cool", 65);

            //assert
            Assert.Equal(20, controller.DeviceId);
            Assert.Equal("Store HVAC", controller.DeviceName);
            Assert.Equal("Cool", controller.HvacMode);
            Assert.Equal(65, controller.FanSpeedPercent);
            Assert.True(controller.IsDataAvailable);
        }

        [Theory]
        [InlineData(-10, 0)]
        [InlineData(0, 0)]
        [InlineData(100, 100)]
        [InlineData(150, 100)]
        public void Constructor_OutOfRangeFanSpeed_ClampsToZeroAndHundred(int inputSpeed, int expectedSpeed)
        {
            //arrange
            //act
            var controller = new VentilationController(20, "Store HVAC", "Auto", inputSpeed);

            //assert
            Assert.Equal(expectedSpeed, controller.FanSpeedPercent);
        }

        [Theory]
        [InlineData("Turbo")]
        [InlineData("")]
        [InlineData("55")]
        public void Constructor_InvalidHvacMode_DefaultsToOff(string invalidMode)
        {
            //arrange
            //act
            var controller = new VentilationController(20, "Store HVAC", invalidMode, 50);

            //assert
            Assert.Equal("Off", controller.HvacMode);
        }
        
        [Theory]
        [InlineData("Auto", "Auto")]
        [InlineData("auto", "Auto")]
        [InlineData("Cool", "Cool")]
        [InlineData("COOL", "Cool")]
        [InlineData("Boost", "Boost")]
        [InlineData("boost", "Boost")]
        [InlineData("Heat", "Heat")]
        [InlineData("HEAT", "Heat")]
        [InlineData("Off", "Off")]
        [InlineData("off", "Off")]
        public void SetConfig_ValidHvacModes_SucceedsAndUpdatesState(string requested, string expectedMode)
        {
            //arrange
            var controller = new VentilationController(20, "Store HVAC", "Auto", 60);
            var command = new CommandRequest(20, requested, "admin-01");

            //act
            CommandResult result = controller.SetConfig(command);

            //assert
            Assert.True(result.IsSuccess);
            Assert.Equal(expectedMode, result.ConfirmedState);
            Assert.Null(result.ErrorMessage);
            Assert.Equal(expectedMode, controller.HvacMode);
            Assert.Equal(60, controller.FanSpeedPercent);
        }

        [Theory]
        [InlineData("0", 0)]
        [InlineData("50", 50)]
        [InlineData("100", 100)]
        [InlineData(" 75 ", 75)]
        public void SetConfig_ValidFanSpeed_UpdatesFanSpeedPercent(string requested, int expectedSpeed)
        {
            //arrange
            var controller = new VentilationController(20, "Store HVAC", "Cool", 30);
            var command = new CommandRequest(20, requested, "admin-01");

            //act
            CommandResult result = controller.SetConfig(command);

            //assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Cool", result.ConfirmedState);
            Assert.Null(result.ErrorMessage);
            Assert.Equal(expectedSpeed, controller.FanSpeedPercent);
        }

        [Fact]
        public void SetConfig_FanSpeedGreaterThanZeroWhenOff_SwitchesModeToAuto()
        {
            //arrange
            var controller = new VentilationController(20, "Store HVAC", "Off", 0);
            var command = new CommandRequest(20, "60", "admin-01");

            //act
            CommandResult result = controller.SetConfig(command);

            //assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Auto", result.ConfirmedState);
            Assert.Equal("Auto", controller.HvacMode);
            Assert.Equal(60, controller.FanSpeedPercent);
        }

        [Fact]
        public void SetConfig_FanSpeedZeroWhenOff_KeepsModeOff()
        {
            //arrange
            var controller = new VentilationController(20, "Store HVAC", "Off", 40);
            var command = new CommandRequest(20, "0", "admin-01");

            //act
            CommandResult result = controller.SetConfig(command);

            //assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Off", result.ConfirmedState);
            Assert.Equal("Off", controller.HvacMode);
            Assert.Equal(0, controller.FanSpeedPercent);
        }

        [Fact]
        public void SetConfig_NullCommandOrNullState_IsRejectedAndStateUnchanged()
        {
            //arrange
            var controller = new VentilationController(20, "Store HVAC", "Cool", 55);
            var nullStateCommand = new CommandRequest(20, null!, "admin-01");

            //act
            CommandResult nullCmdResult = controller.SetConfig(null!);
            CommandResult nullStateResult = controller.SetConfig(nullStateCommand);

            //assert
            Assert.False(nullCmdResult.IsSuccess);
            Assert.Equal("Cool", nullCmdResult.ConfirmedState);
            Assert.NotNull(nullCmdResult.ErrorMessage);

            Assert.False(nullStateResult.IsSuccess);
            Assert.Equal("Cool", nullStateResult.ConfirmedState);
            Assert.NotNull(nullStateResult.ErrorMessage);

            Assert.Equal("Cool", controller.HvacMode);
            Assert.Equal(55, controller.FanSpeedPercent);
        }

        [Fact]
        public void SetConfig_WrongDeviceId_IsRejectedAndStateUnchanged()
        {
            //arrage
            var controller = new VentilationController(20, "Store HVAC", "Auto", 50);
            var command = new CommandRequest(999, "Boost", "admin-01");

            //act
            CommandResult result = controller.SetConfig(command);

            //assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Auto", result.ConfirmedState);
            Assert.NotNull(result.ErrorMessage);
            Assert.Equal("Auto", controller.HvacMode);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void SetConfig_MissingOrWhitespaceAdminUserId_IsRejectedAndStateUnchanged(string? adminUserId)
        {
            //arrange
            var controller = new VentilationController(20, "Store HVAC", "Auto", 50);
            var command = new CommandRequest(20, "Boost", adminUserId!);

            //act
            CommandResult result = controller.SetConfig(command);

            //assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Auto", result.ConfirmedState);
            Assert.NotNull(result.ErrorMessage);
            Assert.Equal("Auto", controller.HvacMode);
        }

        [Theory]
        [InlineData("Turbo")]
        [InlineData("-5")]
        [InlineData("101")]
        [InlineData("abc")]
        [InlineData("")]
        public void SetConfig_UnsupportedCommandOrOutOfRangeSpeed_IsRejectedAndStateUnchanged(string invalidCommand)
        {
            //arrange
            var controller = new VentilationController(20, "Store HVAC", "Heat", 45);
            var command = new CommandRequest(20, invalidCommand, "admin-01");

            //act
            CommandResult result = controller.SetConfig(command);

            //assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Heat", result.ConfirmedState);
            Assert.NotNull(result.ErrorMessage);
            Assert.Equal("Heat", controller.HvacMode);
            Assert.Equal(45, controller.FanSpeedPercent);
        }

        [Fact]
        public void GetTelemetry_WhenActive_ReturnsCurrentFanSpeedAndPercentUnit()
        {
            //arrange
            var controller = new VentilationController(42, "Store HVAC", "Boost", 85);

            //act
            TelemetryPayload telemetry = controller.GetTelemetry();

            //assert
            Assert.Equal(42, telemetry.DeviceId);
            Assert.Equal(85, telemetry.Value);
            Assert.Equal("%", telemetry.Unit);
            Assert.Equal(DeviceStatus.Normal, telemetry.Status);
            Assert.True(telemetry.IsDataAvailable);
        }

        [Fact]
        public void GetTelemetry_WhenOff_ReturnsZeroFanSpeed()
        {
            //arrange
            var controller = new VentilationController(42, "Store HVAC", "Off", 70);

            //act
            TelemetryPayload telemetry = controller.GetTelemetry();

            //assert
            Assert.Equal(42, telemetry.DeviceId);
            Assert.Equal(0, telemetry.Value);
            Assert.Equal("%", telemetry.Unit);
        }
    }
}
