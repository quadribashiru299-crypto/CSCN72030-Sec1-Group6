using SmartShop.Devices;
using SmartShop.Models;

namespace SmartShop.Tests.Devices
{
    public class OccupancySensorTests
    {
        [Fact]
        public void Constructor_UsesSharedDeviceContract()
        {
            using var data = new TempDataFile(
                "OccupancySensor|10|count|Store Occupancy");

            var sensor = new OccupancySensor(data.Path);

            Assert.Equal(5, sensor.DeviceId);
            Assert.Equal("Store Occupancy", sensor.DeviceName);
            Assert.Equal("count", sensor.Unit);
            Assert.Equal(15, sensor.Thresholds.HighWarning);
            Assert.Equal(20, sensor.Thresholds.HighCritical);
            Assert.False(sensor.IsDataAvailable);
        }

        [Fact]
        public void ReadData_LoopsAndClassifiesHighThresholds()
        {
            using var data = new TempDataFile(
                "OccupancySensor|14|count|Store Occupancy",
                "OccupancySensor|15|count|Store Occupancy",
                "OccupancySensor|20|count|Store Occupancy");

            var sensor = new OccupancySensor(data.Path);

            sensor.ReadData();
            Assert.Equal(14, sensor.CurrentOccupancy);
            Assert.Equal(DeviceStatus.Normal, sensor.Status);

            sensor.ReadData();
            Assert.Equal(15, sensor.CurrentOccupancy);
            Assert.Equal(DeviceStatus.Warning, sensor.Status);

            sensor.ReadData();
            Assert.Equal(20, sensor.CurrentOccupancy);
            Assert.Equal(DeviceStatus.Critical, sensor.Status);

            sensor.ReadData();
            Assert.Equal(14, sensor.CurrentOccupancy);
            Assert.Equal(DeviceStatus.Normal, sensor.Status);
        }

        [Fact]
        public void InvalidReading_KeepsLastKnownValueAndMarksUnavailable()
        {
            using var data = new TempDataFile(
                "OccupancySensor|9|count|Store Occupancy",
                "OccupancySensor|9.5|count|Store Occupancy");

            var sensor = new OccupancySensor(data.Path);

            sensor.ReadData();
            Assert.Equal(9, sensor.CurrentOccupancy);
            Assert.True(sensor.IsDataAvailable);

            sensor.ReadData();

            Assert.Equal(9, sensor.CurrentOccupancy);
            Assert.False(sensor.IsDataAvailable);
            Assert.Equal(DeviceStatus.Warning, sensor.Status);
            Assert.NotNull(sensor.LastReadError);
        }

        [Fact]
        public void MissingFile_DoesNotThrowAndMarksUnavailable()
        {
            var sensor = new OccupancySensor(TempDataFile.MissingPath());

            Exception? exception = Record.Exception(sensor.ReadData);

            Assert.Null(exception);
            Assert.False(sensor.IsDataAvailable);
            Assert.NotNull(sensor.LastReadError);
        }

        [Fact]
        public void GetTelemetry_ReturnsSharedPayload()
        {
            using var data = new TempDataFile(
                "OccupancySensor|17|count|Store Occupancy");

            var sensor = new OccupancySensor(data.Path);
            sensor.ReadData();

            TelemetryPayload telemetry = sensor.GetTelemetry();

            Assert.Equal(sensor.DeviceId, telemetry.DeviceId);
            Assert.Equal(17d, telemetry.Value);
            Assert.Equal("count", telemetry.Unit);
            Assert.Equal(DeviceStatus.Warning, telemetry.Status);
            Assert.True(telemetry.IsDataAvailable);
        }
    }
}
