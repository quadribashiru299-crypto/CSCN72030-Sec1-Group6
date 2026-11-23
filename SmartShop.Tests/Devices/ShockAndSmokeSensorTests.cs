using SmartShop.Devices;
using SmartShop.Models;

namespace SmartShop.Tests.Devices;

public class ShockAndSmokeSensorTests
{
    [Fact]
    public void ShockSensor_UsesThresholdsAndLoopsBackToTheFirstSample()
    {
        using var data = new TempDataFile(
            "# device|value|unit",
            "ShockSensor|0.5|g",
            "ShockSensor|1|g",
            "ShockSensor|2|g");
        var sensor = new ShockSensor(data.Path);

        sensor.ReadData();
        Assert.Equal(DeviceStatus.Normal, sensor.Status);
        Assert.Equal(0.5, sensor.Value);

        sensor.ReadData();
        Assert.Equal(DeviceStatus.Warning, sensor.Status);

        sensor.ReadData();
        Assert.Equal(DeviceStatus.Critical, sensor.Status);

        sensor.ReadData();
        Assert.Equal(DeviceStatus.Normal, sensor.Status);
        Assert.Equal(0.5, sensor.Value);
    }

    [Fact]
    public void SmokeDetector_PublishesTelemetryUsingConfiguredThresholds()
    {
        using var data = new TempDataFile("SmokeDetector|50|ppm");
        var sensor = new SmokeDetector(data.Path);

        DateTime beforeRead = DateTime.UtcNow;
        sensor.ReadData();
        DateTime afterRead = DateTime.UtcNow;
        TelemetryPayload first = sensor.GetTelemetry();
        Assert.Equal(7, first.DeviceId);
        Assert.Equal("ppm", first.Unit);
        Assert.Equal(50d, first.Value);
        Assert.Equal(DeviceStatus.Critical, first.Status);
        Assert.True(first.IsDataAvailable);
        Assert.Equal(DateTimeKind.Utc, first.Timestamp.Kind);
        Assert.InRange(first.Timestamp, beforeRead, afterRead);

        var thresholds = sensor.Thresholds;
        thresholds.HighCritical = 60;
        sensor.Thresholds = thresholds;
        sensor.ReadData();
        Assert.Equal(DeviceStatus.Warning, sensor.Status);
    }

    [Theory]
    [InlineData("SmokeDetector|NaN|ppm")]
    [InlineData("SmokeDetector|Infinity|ppm")]
    [InlineData("SmokeDetector|-1|ppm")]
    [InlineData("ShockSensor|15|ppm")]
    [InlineData("SmokeDetector|15|g")]
    [InlineData("SmokeDetector|15")]
    public void SmokeDetector_RetainsTheLastValidSampleAndRecovers(string invalidRecord)
    {
        using var data = new TempDataFile(
            "SmokeDetector|10|ppm",
            invalidRecord,
            "SmokeDetector|30|ppm");
        var sensor = new SmokeDetector(data.Path);
        sensor.ReadData();
        TelemetryPayload first = sensor.GetTelemetry();

        sensor.ReadData();
        TelemetryPayload unavailable = sensor.GetTelemetry();

        Assert.False(unavailable.IsDataAvailable);
        Assert.Equal(first.Value, unavailable.Value);
        Assert.Equal(first.Timestamp, unavailable.Timestamp);
        Assert.Equal(DeviceStatus.Warning, unavailable.Status);
        Assert.NotNull(sensor.LastReadError);

        sensor.ReadData();
        Assert.True(sensor.IsDataAvailable);
        Assert.Equal(30d, sensor.Value);
        Assert.Null(sensor.LastReadError);
    }

    [Fact]
    public void ShockSensor_ReportsMissingFileWithoutInventingAReading()
    {
        var sensor = new ShockSensor(TempDataFile.MissingPath());

        sensor.ReadData();

        Assert.False(sensor.IsDataAvailable);
        Assert.Null(sensor.Value);
        Assert.NotNull(sensor.LastReadError);
    }
}
