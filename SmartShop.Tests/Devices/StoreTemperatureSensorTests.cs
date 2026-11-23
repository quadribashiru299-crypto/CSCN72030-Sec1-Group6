using SmartShop.Devices;
using SmartShop.Models;
using System;
using System.Collections.Generic;
using System.Text;

// the sensor doesn't read any file yet

namespace SmartShop.Tests.Devices
{
    public class StoreTemperatureSensorTests
    {
        [Theory]
        [InlineData(21.5, true)]
        [InlineData(-5.0, false)]
        [InlineData(60.0, false)]
        [InlineData(44.9, true)]
        public void Validate_TemperatureValue_ReturnsExpected(double temp, bool expected)
        {
            //arrange
            var sensor = new StoreTemperatureSensor(1, "Store Temp", "test.txt");
            sensor.Value = temp;

            //act
            bool result = sensor.Validate();

            //assert
            Assert.Equal(expected, result);
        }

        //Validate_NullOrNaNTemperature_ReturnsFalse(null, double.NaN, double.PositiveInfinity)
        

        [Theory]
        [InlineData(21.0, DeviceStatus.Normal)]
        [InlineData(17.5, DeviceStatus.Warning)]
        [InlineData(25.5, DeviceStatus.Warning)]
        [InlineData(14.0, DeviceStatus.Critical)]
        [InlineData(29.0, DeviceStatus.Critical)]
        public void ClassifyStatus_TemperatureValue_ReturnsExpectedStatus(double temp, DeviceStatus expected)
        {
            //arrange
            var sensor = new StoreTemperatureSensor(1, "Store Temp", "test.txt");
            sensor.Value = temp;
            sensor.IsDataAvailable = true;

            //act
            DeviceStatus result = sensor.ClassifyStatus();

            //assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetTelemetry_ValidReading_ReturnsMatchingPayload()
        {
            //arrange
            var sensor = new StoreTemperatureSensor(1, "Store Temperature Sensor 1", "test.txt");
            sensor.Value = 21.5;
            sensor.Humidity = 45.0;
            sensor.IsDataAvailable = sensor.Validate();
            sensor.Status = sensor.ClassifyStatus();

            //act
            TelemetryPayload payload = sensor.GetTelemetry();

            //assert
            Assert.Equal(1, payload.DeviceId);
            Assert.Equal(21.5, payload.Value);
            Assert.Equal("°C", payload.Unit);
            Assert.Equal(DeviceStatus.Normal, payload.Status);
            Assert.True(payload.IsDataAvailable);
        }

        [Fact]
        public void Constructor_SetsDefaultUnitAndThresholds()
        {
            //arrange
            //act
            var sensor = new StoreTemperatureSensor(1, "Store Temperature Sensor 6", "test.txt");

            //assert
            Assert.Equal(1, sensor.DeviceId);
            Assert.Equal("Store Temperature Sensor 6", sensor.DeviceName);
            Assert.Equal("°C", sensor.Unit);
            Assert.Equal(16.0, sensor.Thresholds.LowCritical);
            Assert.Equal(19.0, sensor.Thresholds.LowWarning);
            Assert.Equal(24.0, sensor.Thresholds.HighWarning);
            Assert.Equal(27.0, sensor.Thresholds.HighCritical);
        }
    }
}

