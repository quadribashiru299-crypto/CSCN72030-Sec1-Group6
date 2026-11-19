using SmartShop.Core;
using SmartShop.Models;
using SmartShop.Services;

namespace SmartShop.Tests.Services;

public class ThresholdEngineTests
{
    /// <summary>Threshold set shaped like the refrigerator sensor.</summary>
    private static readonly ThresholdConfig Fridge = new()
    {
        LowCritical = 2.0,
        LowWarning = 3.0,
        HighWarning = 4.0,
        HighCritical = 5.0,
    };

    [Theory]
    [InlineData(4.0, DeviceStatus.Warning)]   // exactly at HighWarning
    [InlineData(5.0, DeviceStatus.Critical)]  // exactly at HighCritical
    [InlineData(6.0, DeviceStatus.Critical)]  // above HighCritical
    [InlineData(3.0, DeviceStatus.Warning)]   // exactly at LowWarning
    [InlineData(2.0, DeviceStatus.Critical)]  // exactly at LowCritical
    [InlineData(1.0, DeviceStatus.Critical)]  // below LowCritical
    [InlineData(3.5, DeviceStatus.Normal)]    // inside the normal band
    public void Classify_ThresholdBoundaries_ReturnExpectedStatus(double value, DeviceStatus expected)
    {
        // Arrange
        var config = Fridge;

        // Act
        var status = ThresholdEngine.Classify(config, value);

        // Assert
        Assert.Equal(expected, status);
    }

    [Fact]
    public void Classify_ValueMatchingBothBands_PrefersCritical()
    {
        // Arrange
        var overlap = new ThresholdConfig
        {
            LowWarning = 3.0,
            LowCritical = 3.5,
        };

        // Act
        var status = ThresholdEngine.Classify(overlap, 3.4);

        // Assert
        Assert.Equal(DeviceStatus.Critical, status);
    }

    [Fact]
    public void Classify_UnknownSensorId_FallsBackToNormal()
    {
        // Arrange
        var engine = new ThresholdEngine(new Dictionary<string, ThresholdConfig>());

        // Act
        var status = engine.Classify("NoSuchSensor", 999.0);

        // Assert
        Assert.Equal(DeviceStatus.Normal, status);
    }

    [Fact]
    public void Classify_KnownSensorId_UsesItsThresholdSet()
    {
        // Arrange
        var engine = new ThresholdEngine(new Dictionary<string, ThresholdConfig>
        {
            ["RefrigeratorTemperature"] = Fridge,
        });

        // Act
        var critical = engine.Classify("RefrigeratorTemperature", 5.0);
        var normal = engine.Classify("RefrigeratorTemperature", 3.5);

        // Assert
        Assert.Equal(DeviceStatus.Critical, critical);
        Assert.Equal(DeviceStatus.Normal, normal);
    }
}
