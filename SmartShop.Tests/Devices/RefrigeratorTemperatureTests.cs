using SmartShop.Core;
using SmartShop.Devices;
using SmartShop.Models;
using Xunit;

namespace SmartShop.Tests.Devices
{
    public class RefrigeratorTemperatureTests
    {
        private static TempDataFile CreateFile(params string[] values)
        {
            var lines = new string[values.Length];

            for (int i = 0; i < values.Length; i++)
            {
                lines[i] = $"RefrigeratorTemperature|{values[i]}|C";
            }

            return new TempDataFile(lines);
        }

        private static TempDataFile DefaultFile()
        {
            return CreateFile(
                "3.6", "3.8", "4.0", "4.5", "5.2", "5.8",
                "3.5", "3.0", "2.6", "1.8", "2.4", "3.7");
        }

        private static double Reading(Sensor sensor) => sensor.Value ?? double.NaN;

        [Fact]
        public void FirstRead_PublishesTemperatureAndNormalStatus()
        {
            using var file = DefaultFile();
            var sensor = new RefrigeratorTemperature(file.Path);

            sensor.ReadData();

            Assert.True(sensor.IsDataAvailable);
            Assert.Equal(3.6, Reading(sensor));
            Assert.Equal("C", sensor.Unit);
            Assert.Equal(DeviceStatus.Normal, sensor.Status);
        }

        [Fact]
        public void ReadingFourDegrees_IsWarning()
        {
            using var file = DefaultFile();
            var sensor = new RefrigeratorTemperature(file.Path);

            for (int i = 0; i < 3; i++)
            {
                sensor.ReadData();
            }

            Assert.Equal(4.0, Reading(sensor));
            Assert.Equal(DeviceStatus.Warning, sensor.Status);
        }

        [Fact]
        public void ReadingFivePointTwo_IsCritical()
        {
            using var file = DefaultFile();
            var sensor = new RefrigeratorTemperature(file.Path);

            for (int i = 0; i < 5; i++)
            {
                sensor.ReadData();
            }

            Assert.Equal(5.2, Reading(sensor));
            Assert.Equal(DeviceStatus.Critical, sensor.Status);
        }

        [Fact]
        public void ReadingThreeDegrees_IsWarning()
        {
            using var file = DefaultFile();
            var sensor = new RefrigeratorTemperature(file.Path);

            for (int i = 0; i < 8; i++)
            {
                sensor.ReadData();
            }

            Assert.Equal(3.0, Reading(sensor));
            Assert.Equal(DeviceStatus.Warning, sensor.Status);
        }

        [Fact]
        public void ReadingOnePointEight_IsCritical()
        {
            using var file = DefaultFile();
            var sensor = new RefrigeratorTemperature(file.Path);

            for (int i = 0; i < 10; i++)
            {
                sensor.ReadData();
            }

            Assert.Equal(1.8, Reading(sensor));
            Assert.Equal(DeviceStatus.Critical, sensor.Status);
        }

        [Fact]
        public void ReadingPastTheEnd_WrapsToTheFirstRecord()
        {
            using var file = DefaultFile();
            var sensor = new RefrigeratorTemperature(file.Path);

            for (int i = 0; i < 13; i++)
            {
                sensor.ReadData();
            }

            Assert.Equal(3.6, Reading(sensor));
            Assert.True(sensor.IsDataAvailable);
        }

        [Fact]
        public void MissingFile_StaysUnavailableWithoutThrowing()
        {
            var sensor = new RefrigeratorTemperature(TempDataFile.MissingPath());

            sensor.ReadData();

            Assert.False(sensor.IsDataAvailable);
            Assert.Null(sensor.Value);
            Assert.Equal(DeviceStatus.Warning, sensor.Status);
            Assert.NotNull(sensor.LastSimulationError);
        }

        [Fact]
        public void RecordForAnotherDevice_IsRejected()
        {
            using var file = new TempDataFile("ShelfStockSensor|40|count");
            var sensor = new RefrigeratorTemperature(file.Path);

            sensor.ReadData();

            Assert.False(sensor.IsDataAvailable);
            Assert.Null(sensor.Value);
        }

        [Fact]
        public void NonNumericValue_IsRejected()
        {
            using var file = CreateFile("n/a");
            var sensor = new RefrigeratorTemperature(file.Path);

            sensor.ReadData();

            Assert.False(sensor.IsDataAvailable);
            Assert.Null(sensor.Value);
        }

        [Fact]
        public void PhysicallyImpossibleValue_IsRejected()
        {
            using var file = CreateFile("25");
            var sensor = new RefrigeratorTemperature(file.Path);

            sensor.ReadData();

            Assert.False(sensor.IsDataAvailable);
            Assert.Null(sensor.Value);
        }

        [Fact]
        public void Telemetry_CarriesDeviceIdentityAndReading()
        {
            using var file = DefaultFile();
            var sensor = new RefrigeratorTemperature(file.Path);

            TelemetryPayload payload = sensor.GetTelemetry();

            Assert.Equal(2, payload.DeviceId);
            Assert.Equal(3.6, payload.Value ?? double.NaN);
            Assert.Equal("C", payload.Unit);
            Assert.True(payload.IsDataAvailable);
        }

        [Fact]
        public void InjectedThresholds_ReplaceTheDefaults()
        {
            using var file = CreateFile("4.0");
            var custom = new ThresholdConfig
            {
                LowCritical = 2.0,
                LowWarning = 3.0,
                HighWarning = 6.0,
                HighCritical = 7.0
            };
            var sensor = new RefrigeratorTemperature(file.Path, thresholds: custom);

            sensor.ReadData();

            Assert.Equal(DeviceStatus.Normal, sensor.Status);
        }
    }
}
