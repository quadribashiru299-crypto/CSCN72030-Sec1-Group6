using SmartShop.Core;
using SmartShop.Devices;
using SmartShop.Models;

namespace SmartShop.Tests.Devices
{
    public class CheckoutSensorTests
    {
        private static string Line(double value, string sensor = CheckoutSensor.SensorKey) =>
            $"2026-11-16T09:00:00Z|{sensor}|{value.ToString(System.Globalization.CultureInfo.InvariantCulture)}|count|Checkout Activity";

        [Fact]
        public void ReadData_ValidRecord_PublishesValueAndMarksAvailable()
        {
            using var file = new TempDataFile(Line(3));
            var sensor = new CheckoutSensor(file.Path);

            sensor.ReadData();
            TelemetryPayload telemetry = sensor.GetTelemetry();

            Assert.Equal(CheckoutSensor.DefaultDeviceId, telemetry.DeviceId);
            Assert.Equal(3, telemetry.Value);
            Assert.Equal("count", telemetry.Unit);
            Assert.True(telemetry.IsDataAvailable);
            Assert.Equal(DeviceStatus.Normal, telemetry.Status);
            Assert.Null(sensor.LastReadError);
        }

        [Fact]
        public void ReadData_AfterLastRecord_RestartsAtFirstRecord()
        {
            using var file = new TempDataFile(Line(1), Line(2), Line(4));
            var sensor = new CheckoutSensor(file.Path);

            var values = new List<double?>();
            for (int i = 0; i < 5; i++)
            {
                sensor.ReadData();
                values.Add(sensor.Value);
            }

            Assert.Equal(new double?[] { 1, 2, 4, 1, 2 }, values);
        }

        [Fact]
        public void ReadData_SkipsCommentsAndBlankLines()
        {
            using var file = new TempDataFile("# header", "", Line(5), "   ", "# trailing");
            var sensor = new CheckoutSensor(file.Path);

            sensor.ReadData();

            Assert.Equal(5, sensor.Value);
            Assert.True(sensor.IsDataAvailable);
        }

        [Fact]
        public void ReadData_MissingFile_MarksUnavailableWithoutThrowing()
        {
            var sensor = new CheckoutSensor(TempDataFile.MissingPath());

            Exception? error = Record.Exception(() => sensor.ReadData());

            Assert.Null(error);
            Assert.False(sensor.IsDataAvailable);
            Assert.False(sensor.GetTelemetry().IsDataAvailable);
            Assert.NotNull(sensor.LastReadError);
        }

        [Fact]
        public void ReadData_EmptyFile_MarksUnavailable()
        {
            using var file = new TempDataFile("# only a comment");
            var sensor = new CheckoutSensor(file.Path);

            sensor.ReadData();

            Assert.False(sensor.IsDataAvailable);
            Assert.NotNull(sensor.LastReadError);
        }

        [Theory]
        [InlineData("not a record")]
        [InlineData("2026-11-16T09:00:00Z|CheckoutSensor|abc|count|Checkout Activity")]
        [InlineData("2026-11-16T09:00:00Z|CheckoutSensor|-1|count|Checkout Activity")]
        [InlineData("2026-11-16T09:00:00Z|CheckoutSensor|2.5|count|Checkout Activity")]
        [InlineData("2026-11-16T09:00:00Z|CheckoutSensor|500|count|Checkout Activity")]
        [InlineData("2026-11-16T09:00:00Z|ShelfStockSensor|3|count|Shelf")]
        [InlineData("yesterday|CheckoutSensor|3|count|Checkout Activity")]
        public void ReadData_InvalidRecord_KeepsLastKnownValueAndMarksUnavailable(string badRecord)
        {
            using var file = new TempDataFile(Line(4), badRecord);
            var sensor = new CheckoutSensor(file.Path);

            sensor.ReadData(); // valid: 4
            sensor.ReadData(); // invalid

            Assert.Equal(4, sensor.Value); // last known state retained
            Assert.False(sensor.IsDataAvailable);
            Assert.NotNull(sensor.LastReadError);
        }

        [Fact]
        public void ReadData_ValidRecordAfterInvalid_BecomesAvailableAgain()
        {
            using var file = new TempDataFile("garbage", Line(2));
            var sensor = new CheckoutSensor(file.Path);

            sensor.ReadData();
            sensor.ReadData();

            Assert.True(sensor.IsDataAvailable);
            Assert.Equal(2, sensor.Value);
            Assert.Null(sensor.LastReadError);
        }

        [Theory]
        [InlineData(0.0, DeviceStatus.Normal)]
        [InlineData(5.0, DeviceStatus.Normal)]
        [InlineData(6.0, DeviceStatus.Warning)]
        [InlineData(8.0, DeviceStatus.Warning)]
        [InlineData(9.0, DeviceStatus.Critical)]
        [InlineData(15.0, DeviceStatus.Critical)]
        public void ReadData_ClassifiesAgainstDefaultThresholds(double reading, DeviceStatus expected)
        {
            using var file = new TempDataFile(Line(reading));
            var sensor = new CheckoutSensor(file.Path);

            sensor.ReadData();

            Assert.Equal(expected, sensor.Status);
        }

        [Fact]
        public void ReadData_ReturnsToNormal_WhenReadingDropsBelowWarning()
        {
            using var file = new TempDataFile(Line(9), Line(2));
            var sensor = new CheckoutSensor(file.Path);

            sensor.ReadData();
            Assert.Equal(DeviceStatus.Critical, sensor.Status);

            sensor.ReadData();
            Assert.Equal(DeviceStatus.Normal, sensor.Status);
        }

        [Fact]
        public void Thresholds_AreConfigurable()
        {
            using var file = new TempDataFile(Line(4));
            var sensor = new CheckoutSensor(file.Path);
            sensor.Thresholds = new ThresholdConfig { HighWarning = 3, HighCritical = 10 };

            sensor.ReadData();

            Assert.Equal(DeviceStatus.Warning, sensor.Status);
        }

        [Fact]
        public void RecordTransaction_AddsActivityAndCountsPurchase()
        {
            using var file = new TempDataFile(Line(5));
            var sensor = new CheckoutSensor(file.Path);
            sensor.ReadData();

            TelemetryPayload telemetry = sensor.RecordTransaction();

            Assert.Equal(6, telemetry.Value);
            Assert.Equal(DeviceStatus.Warning, telemetry.Status);
            Assert.True(telemetry.IsDataAvailable);
            Assert.Equal(1, sensor.TotalTransactions);
        }

        [Fact]
        public void RecordTransaction_BeforeAnyReading_StartsFromZero()
        {
            var sensor = new CheckoutSensor(TempDataFile.MissingPath());

            TelemetryPayload telemetry = sensor.RecordTransaction();

            Assert.Equal(1, telemetry.Value);
            Assert.True(telemetry.IsDataAvailable);
        }

        [Fact]
        public void Constructor_UsesGivenDeviceId()
        {
            var sensor = new CheckoutSensor(TempDataFile.MissingPath(), deviceId: 42);

            Assert.Equal(42, sensor.GetTelemetry().DeviceId);
        }

        [Fact]
        public void Constructor_NullPath_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new CheckoutSensor(null!));
        }
    }
}
