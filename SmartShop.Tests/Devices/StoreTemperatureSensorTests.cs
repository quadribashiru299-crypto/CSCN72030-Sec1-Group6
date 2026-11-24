using SmartShop.Core;
using SmartShop.Devices;
using SmartShop.Models;
using Xunit;

namespace SmartShop.Tests.Devices
{
    public class StoreTemperatureSensorTests
    {
        private static TempDataFile CreateFile(params string[] values)
        {
            var lines = new string[values.Length];

            for (int i = 0; i < values.Length; i++)
            {
                lines[i] = $"StoreTemperatureSensor|{values[i]}|C";
            }

            return new TempDataFile(lines);
        }

        private static double Reading(Sensor sensor) => sensor.Value ?? double.NaN;

        [Fact]
        public void ReadFirstRecord_PublishesTemperatureAndNormalStatus()
        {
            using var file = CreateFile("22.5");
            var sensor = new StoreTemperatureSensor(file.Path, deviceId: 1, deviceName: "Store Temp");

            sensor.ReadData();

            Assert.True(sensor.IsDataAvailable);
            Assert.Equal(22.5, Reading(sensor));
            Assert.Equal("C", sensor.Unit);
            Assert.Equal(DeviceStatus.Normal, sensor.Status);
            Assert.Equal(1, sensor.DeviceId);
            Assert.Equal("Store Temp", sensor.DeviceName);
        }

        [Fact]
        public void ReadingAboveHighWarning_IsWarning()
        {
            using var file = CreateFile("25.5");
            var sensor = new StoreTemperatureSensor(file.Path);

            sensor.ReadData();

            Assert.Equal(DeviceStatus.Warning, sensor.Status);
        }

        [Fact]
        public void ReadingAboveHighCritical_IsCritical()
        {
            using var file = CreateFile("29.0");
            var sensor = new StoreTemperatureSensor(file.Path);

            sensor.ReadData();

            Assert.Equal(29.0, Reading(sensor));
            Assert.Equal(DeviceStatus.Critical, sensor.Status);
        }

        [Fact]
        public void ReadingBelowLowWarning_IsWarning()
        {
            using var file = CreateFile("17.5");
            var sensor = new StoreTemperatureSensor(file.Path);

            sensor.ReadData();

            Assert.Equal(DeviceStatus.Warning, sensor.Status);
        }

        [Fact]
        public void ReadingBelowLowCritical_IsCritical()
        {
            using var file = CreateFile("14.0");
            var sensor = new StoreTemperatureSensor(file.Path);

            sensor.ReadData();

            Assert.Equal(DeviceStatus.Critical, sensor.Status);
        }

        [Fact]
        public void ReadingAtUpperLimit_IsRejected()
        {
            using var file = CreateFile("45.0");
            var sensor = new StoreTemperatureSensor(file.Path);

            sensor.ReadData();

            Assert.False(sensor.IsDataAvailable);
            Assert.Null(sensor.Value);
        }

        [Fact]
        public void ReadingAtLowerLimit_IsRejected()
        {
            using var file = CreateFile("-5.0");
            var sensor = new StoreTemperatureSensor(file.Path);

            sensor.ReadData();

            Assert.False(sensor.IsDataAvailable);
            Assert.Null(sensor.Value);
        }

        [Fact]
        public void MissingFile_StaysUnavailableWithoutThrowing()
        {
            var sensor = new StoreTemperatureSensor(TempDataFile.MissingPath());

            sensor.ReadData();

            Assert.False(sensor.IsDataAvailable);
            Assert.Null(sensor.Value);
            Assert.NotNull(sensor.LastSimulationError);
        }

        [Fact]
        public void RecordForAnotherDevice_IsRejected()
        {
            using var file = new TempDataFile("RefrigeratorTemperature|4.0|C");
            var sensor = new StoreTemperatureSensor(file.Path);

            sensor.ReadData();

            Assert.False(sensor.IsDataAvailable);
            Assert.Null(sensor.Value);
        }

        [Fact]
        public void GetTelemetry_CarriesDeviceIdentityAndReading()
        {
            using var file = CreateFile("22.5");
            var sensor = new StoreTemperatureSensor(file.Path, deviceName: "Store Temperature Sensor 1");

            TelemetryPayload payload = sensor.GetTelemetry();

            Assert.Equal(1, payload.DeviceId);
            Assert.Equal(22.5, payload.Value ?? double.NaN);
            Assert.Equal("C", payload.Unit);
            Assert.Equal(DeviceStatus.Normal, payload.Status);
            Assert.True(payload.IsDataAvailable);
        }

        [Fact]
        public void Constructor_SetsDefaultThresholds()
        {
            var sensor = new StoreTemperatureSensor("test.txt", deviceId: 1, deviceName: "Store Temperature Sensor 6");

            Assert.Equal(1, sensor.DeviceId);
            Assert.Equal("Store Temperature Sensor 6", sensor.DeviceName);
            Assert.Equal("C", sensor.Unit);
            Assert.Equal(16.0, sensor.Thresholds.LowCritical);
            Assert.Equal(19.0, sensor.Thresholds.LowWarning);
            Assert.Equal(24.0, sensor.Thresholds.HighWarning);
            Assert.Equal(27.0, sensor.Thresholds.HighCritical);
        }

        [Fact]
        public void InjectedThresholds_ReplaceTheDefaults()
        {
            using var file = CreateFile("22.5");
            var custom = new ThresholdConfig
            {
                LowCritical = 10.0,
                LowWarning = 15.0,
                HighWarning = 30.0,
                HighCritical = 35.0
            };
            var sensor = new StoreTemperatureSensor(file.Path, thresholds: custom);

            sensor.ReadData();

            Assert.Equal(DeviceStatus.Normal, sensor.Status);
        }
    }
}
