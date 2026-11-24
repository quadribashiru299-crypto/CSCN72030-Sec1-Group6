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
        var engine = new ThresholdEngine();
        var status = engine.Classify(config, value);

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
        var status = new ThresholdEngine().Classify(overlap, 3.4);

        // Assert
        Assert.Equal(DeviceStatus.Critical, status);
    }

    [Fact]
    public void Classify_UnconfiguredBands_ReturnNormal()
    {
        // Arrange: all four bounds left unset.
        var empty = new ThresholdConfig();

        // Act
        var status = new ThresholdEngine().Classify(empty, 999.0);

        // Assert
        Assert.Equal(DeviceStatus.Normal, status);
    }
}
