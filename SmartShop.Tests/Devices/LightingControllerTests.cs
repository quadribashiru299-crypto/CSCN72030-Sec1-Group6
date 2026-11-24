using SmartShop.Devices;
using SmartShop.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace SmartShop.Tests.Devices
{
    public class LightingControllerTests
    {
        [Fact]
        public void Constructor_ValidParameters_SetsInitialStateAndBrightness()
        {
            //arrange
            //act
            var controller = new LightingController(8, "Store Lighting", true, 75);

            //assert
            Assert.Equal(8, controller.DeviceId);
            Assert.Equal("Store Lighting", controller.DeviceName);
            Assert.True(controller.IsOn);
            Assert.Equal(75, controller.BrightnessPercent);
            Assert.Equal("On", controller.GetConfig().State);
            Assert.Equal(75, controller.GetConfig().Value);
            Assert.True(controller.IsDataAvailable);
        }

        [Theory]
        [InlineData(-10, 0)]
        [InlineData(0, 0)]
        [InlineData(100, 100)]
        [InlineData(150, 100)]
        public void Constructor_OutOfRangeBrightness_ClampsToZeroAndHundred(int inputBrightness, int expectedBrightness)
        {
            //arrange
            //act
            var controller = new LightingController(8, "Store Lighting", true, inputBrightness);

            //assert
            Assert.Equal(expectedBrightness, controller.BrightnessPercent);
        }

        [Theory]
        [InlineData("On")]
        [InlineData("on")]
        [InlineData(" ON ")]
        public void SetConfig_OnCommand_TurnsLightOnAndRetainsPreviousBrightness(string requestedState)
        {
            //arrange
            var controller = new LightingController(8, "Store Lighting", false, 80);
            var cmd = new CommandRequest(8, requestedState, "admin-01");

            //act
            CommandResult result = controller.SetConfig(cmd);

            //assert
            Assert.True(result.IsSuccess);
            Assert.Equal("On", result.ConfirmedState);
            Assert.Null(result.ErrorMessage);
            Assert.True(controller.IsOn);
            Assert.Equal(80, controller.BrightnessPercent);
        }

        [Theory]
        [InlineData("Off")]
        [InlineData("off")]
        [InlineData(" OFF ")]
        public void SetConfig_OffCommand_TurnsLightOffAndRetainsBrightness(string requestedState)
        {
            //arrange
            var controller = new LightingController(8, "Store Lighting", true, 80);
            var cmd = new CommandRequest(8, requestedState, "admin-01");

            //act
            CommandResult result = controller.SetConfig(cmd);

            //assert
            Assert.True(result.IsSuccess);
            Assert.Equal("Off", result.ConfirmedState);
            Assert.Null(result.ErrorMessage);
            Assert.False(controller.IsOn);
            Assert.Equal(80, controller.BrightnessPercent);
        }

        [Theory]
        [InlineData("0", 0)]
        [InlineData("50", 50)]
        [InlineData("100", 100)]
        [InlineData(" 75 ", 75)]
        public void SetConfig_ValidBrightness_UpdatesBrightnessPercent(string requestedState, int expectedBrightness)
        {
            //arrange
            var controller = new LightingController(8, "Store Lighting", true, 40);
            var cmd = new CommandRequest(8, requestedState, "admin-01");

            //act
            CommandResult result = controller.SetConfig(cmd);

            //assert
            Assert.True(result.IsSuccess);
            Assert.Equal("On", result.ConfirmedState);
            Assert.Null(result.ErrorMessage);
            Assert.Equal(expectedBrightness, controller.BrightnessPercent);
        }

        [Fact]
        public void SetConfig_ZeroBrightnessWhenOn_KeepsLightOnWithZeroBrightness()
        {
            //arrange
            var controller = new LightingController(8, "Store Lighting", true, 60);
            var cmd = new CommandRequest(8, "0", "admin-01");

            //act
            CommandResult result = controller.SetConfig(cmd);

            //assert
            Assert.True(result.IsSuccess);
            Assert.True(controller.IsOn);
            Assert.Equal(0, controller.BrightnessPercent);
        }

        [Fact]
        public void SetConfig_NullCommand_IsRejectedAndStateUnchanged()
        {
            //arrange
            var controller = new LightingController(8, "Store Lighting", true, 50);

            //act
            CommandResult result = controller.SetConfig(null!);

            //assert
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.ErrorMessage);
            Assert.True(controller.IsOn);
            Assert.Equal(50, controller.BrightnessPercent);
        }

        [Fact]
        public void SetConfig_NullRequestedState_IsRejectedAndStateUnchanged()
        {
            //arrange
            var controller = new LightingController(8, "Store Lighting", true, 50);
            var cmd = new CommandRequest(8, null!, "admin-01");

            //act
            CommandResult result = controller.SetConfig(cmd);

            //assert
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.ErrorMessage);
            Assert.True(controller.IsOn);
            Assert.Equal(50, controller.BrightnessPercent);
        }

        [Fact]
        public void SetConfig_WrongDeviceId_IsRejectedAndStateUnchanged()
        {
            //arrange
            var controller = new LightingController(8, "Store Lighting", true, 50);
            var cmd = new CommandRequest(99, "Off", "admin-01");

            //act
            CommandResult result = controller.SetConfig(cmd);

            //assert
            Assert.False(result.IsSuccess);
            Assert.Equal("On", result.ConfirmedState);
            Assert.NotNull(result.ErrorMessage);
            Assert.True(controller.IsOn);
            Assert.Equal(50, controller.BrightnessPercent);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void SetConfig_MissingOrWhitespaceAdminUserId_IsRejectedAndStateUnchanged(string? adminUserId)
        {
            //arrange
            var controller = new LightingController(8, "Store Lighting", true, 50);
            var cmd = new CommandRequest(8, "Off", adminUserId!);

            //act
            CommandResult result = controller.SetConfig(cmd);

            //assert
            Assert.False(result.IsSuccess);
            Assert.Equal("On", result.ConfirmedState);
            Assert.NotNull(result.ErrorMessage);
            Assert.True(controller.IsOn);
            Assert.Equal(50, controller.BrightnessPercent);
        }

        [Theory]
        [InlineData("Dim")]
        [InlineData("-1")]
        [InlineData("101")]
        [InlineData("abc")]
        [InlineData("")]
        public void SetConfig_UnsupportedCommandOrOutOfRangeBrightness_IsRejectedAndStateUnchanged(string requestedState)
        {
            //arrange
            var controller = new LightingController(8, "Store Lighting", true, 50);
            var cmd = new CommandRequest(8, requestedState, "admin-01");

            //act
            CommandResult result = controller.SetConfig(cmd);

            //assert
            Assert.False(result.IsSuccess);
            Assert.Equal("On", result.ConfirmedState);
            Assert.NotNull(result.ErrorMessage);
            Assert.True(controller.IsOn);
            Assert.Equal(50, controller.BrightnessPercent);
        }

        [Fact]
        public void GetTelemetry_WhenOn_ReturnsCurrentBrightnessAndPercentUnit()
        {
            //arrange
            var controller = new LightingController(8, "Store Lighting", true, 85);

            //act
            TelemetryPayload payload = controller.GetTelemetry();

            //assert
            Assert.Equal(8, payload.DeviceId);
            Assert.Equal(85, payload.Value);
            Assert.Equal("%", payload.Unit);
            Assert.Equal(DeviceStatus.Normal, payload.Status);
            Assert.True(payload.IsDataAvailable);
        }

        [Fact]
        public void GetTelemetry_WhenOff_ReturnsZeroBrightness()
        {
            //arrange
            var controller = new LightingController(8, "Store Lighting", false, 80);

            //act
            TelemetryPayload payload = controller.GetTelemetry();

            //assert
            Assert.Equal(8, payload.DeviceId);
            Assert.Equal(0, payload.Value);
            Assert.Equal("%", payload.Unit);
            Assert.Equal(80, controller.BrightnessPercent);
        }
    }
}