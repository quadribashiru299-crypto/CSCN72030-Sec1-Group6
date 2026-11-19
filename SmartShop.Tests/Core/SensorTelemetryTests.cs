using SmartShop.Core;
using SmartShop.Models;

namespace SmartShop.Tests.Core
{
    public class SensorTelemetryTests
    {
        /// <summary>2.0 / 3.0 low bands, 8.0 / 10.0 high bands.</summary>
        private static readonly ThresholdConfig Band = new()
        {
            LowCritical = 2.0,
            LowWarning = 3.0,
            HighWarning = 8.0,
            HighCritical = 10.0
        };

        /// <summary>
        /// Minimal sensor: it only declares a file and a range check, and lets the
        /// base class do the reading.
        /// </summary>
        private sealed class StubSensor : Sensor
        {
            public StubSensor(string? dataFilePath)
            {
                DeviceId = 21;
                DeviceName = "Stub Sensor";
                Unit = "C";
                Thresholds = Band;
                SimulationFilePath = dataFilePath;
                IsDataAvailable = false;
                Status = DeviceStatus.Warning;
            }

            public override bool Validate()
            {
                bool isValid = CandidateValue is double value && double.IsFinite(value);
                IsDataAvailable = isValid;
                return isValid;
            }
        }

        [Fact]
        public void GetTelemetry_ReadsNextRecordOnEachCallAndLoops()
        {
            // Arrange
            using var data = new TempDataFile(
                "# Stub sensor looped simulation data",
                "2026-11-17T12:00:00Z|StubSensor|4|C|Stub",
                "2026-11-17T12:00:01Z|StubSensor|9|C|Stub",
                "2026-11-17T12:00:02Z|StubSensor|1|C|Stub");

            var sensor = new StubSensor(data.Path);

            // Act + Assert: one record per GetTelemetry call, cursor wraps.
            Assert.Equal(4d, sensor.GetTelemetry().Value);
            Assert.Equal(DeviceStatus.Normal, sensor.Status);

            Assert.Equal(9d, sensor.GetTelemetry().Value);
            Assert.Equal(DeviceStatus.Warning, sensor.Status);

            Assert.Equal(1d, sensor.GetTelemetry().Value);
            Assert.Equal(DeviceStatus.Critical, sensor.Status);

            Assert.Equal(4d, sensor.GetTelemetry().Value);
            Assert.Equal(DeviceStatus.Normal, sensor.Status);
        }

        [Fact]
        public void GetTelemetry_PublishesSharedContract()
        {
            // Arrange
            using var data = new TempDataFile(
                "2026-11-17T12:00:00Z|StubSensor|4|C|Stub");

            var sensor = new StubSensor(data.Path);

            // Act
            TelemetryPayload payload = sensor.GetTelemetry();

            // Assert
            Assert.Equal(21, payload.DeviceId);
            Assert.Equal(4d, payload.Value);
            Assert.Equal("C", payload.Unit);
            Assert.True(payload.IsDataAvailable);
            Assert.True(payload.Timestamp > default(DateTime));
        }

        [Fact]
        public void MissingFile_DoesNotThrowAndMarksUnavailable()
        {
            // Arrange
            var sensor = new StubSensor(TempDataFile.MissingPath());

            // Act
            Exception? exception = Record.Exception(() => { sensor.GetTelemetry(); });

            // Assert
            Assert.Null(exception);
            Assert.False(sensor.IsDataAvailable);
            Assert.Equal(DeviceStatus.Warning, sensor.Status);
            Assert.NotNull(sensor.LastSimulationError);
        }

        [Fact]
        public void RecordsForAnotherSensor_AreRejected()
        {
            // Arrange
            using var data = new TempDataFile(
                "2026-11-17T12:00:00Z|ShelfStockSensor|42|%|Shelf Stock");

            var sensor = new StubSensor(data.Path);

            // Act
            sensor.GetTelemetry();

            // Assert
            Assert.False(sensor.IsDataAvailable);
            Assert.Null(sensor.Value);
            Assert.NotNull(sensor.LastSimulationError);
        }

        [Fact]
        public void InvalidNumber_KeepsLastKnownValueAndMarksUnavailable()
        {
            // Arrange
            using var data = new TempDataFile(
                "2026-11-17T12:00:00Z|StubSensor|4|C|Stub",
                "2026-11-17T12:00:01Z|StubSensor|n/a|C|Stub");

            var sensor = new StubSensor(data.Path);

            // Act
            sensor.GetTelemetry();
            Assert.Equal(4d, sensor.Value);
            Assert.True(sensor.IsDataAvailable);

            var second = sensor.GetTelemetry();

            // Assert
            Assert.Equal(4d, second.Value);
            Assert.False(second.IsDataAvailable);
            Assert.Equal(DeviceStatus.Warning, second.Status);
        }

        [Fact]
        public void BlankAndCommentLines_AreSkipped()
        {
            // Arrange
            using var data = new TempDataFile(
                "# header",
                "",
                "2026-11-17T12:00:00Z|StubSensor|5|C|Stub",
                "# another comment",
                "2026-11-17T12:00:01Z|StubSensor|6|C|Stub");

            var sensor = new StubSensor(data.Path);

            // Act + Assert
            Assert.Equal(5d, sensor.GetTelemetry().Value);
            Assert.Equal(6d, sensor.GetTelemetry().Value);
            Assert.Equal(5d, sensor.GetTelemetry().Value);
        }
    }
}
