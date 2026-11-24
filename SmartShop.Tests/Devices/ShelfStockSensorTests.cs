using SmartShop.Core;
using SmartShop.Devices;
using SmartShop.Models;
using Xunit;

namespace SmartShop.Tests.Devices
{
    public class ShelfStockSensorTests
    {
        private static TempDataFile LoopFile(params string[] values)
        {
            var lines = new string[values.Length];

            for (int i = 0; i < values.Length; i++)
            {
                lines[i] = $"ShelfStockSensor|{values[i]}|count";
            }

            return new TempDataFile(lines);
        }

        private static double Reading(Sensor sensor) => sensor.Value ?? double.NaN;

        [Fact]
        public void ItemInventory_PublishesTheSumOfAllItems()
        {
            using var items = new TempDataFile("MILK-1L|30", "BREAD|22");
            using var loop = LoopFile("92");
            var sensor = new ShelfStockSensor(loop.Path, items.Path);

            Assert.True(sensor.HasItemInventory);
            Assert.Equal(52, sensor.TotalItems);
            Assert.Equal(52, Reading(sensor));
            Assert.Equal("count", sensor.Unit);
            Assert.Equal(DeviceStatus.Normal, sensor.Status);
            Assert.True(sensor.IsDataAvailable);
        }

        [Fact]
        public void TryRemoveStock_DecreasesThePublishedTotal()
        {
            using var items = new TempDataFile("MILK-1L|30", "BREAD|22");
            using var loop = LoopFile("92");
            var sensor = new ShelfStockSensor(loop.Path, items.Path);

            bool removed = sensor.TryRemoveStock("MILK-1L", 4, out string? error);

            Assert.True(removed);
            Assert.Null(error);
            Assert.Equal(48, Reading(sensor));
            Assert.Equal(48, sensor.TotalItems);
            Assert.True(sensor.IsDataAvailable);
        }

        [Fact]
        public void TryRemoveStock_DroppingBelowWarningThreshold_SetsWarning()
        {
            using var items = new TempDataFile("MILK-1L|25");
            using var loop = LoopFile("92");
            var sensor = new ShelfStockSensor(loop.Path, items.Path);

            sensor.TryRemoveStock("MILK-1L", 6, out _);

            Assert.Equal(19, Reading(sensor));
            Assert.Equal(DeviceStatus.Warning, sensor.Status);
        }

        [Fact]
        public void TryRemoveStock_DroppingBelowCriticalThreshold_SetsCritical()
        {
            using var items = new TempDataFile("MILK-1L|12");
            using var loop = LoopFile("92");
            var sensor = new ShelfStockSensor(loop.Path, items.Path);

            sensor.TryRemoveStock("MILK-1L", 5, out _);

            Assert.Equal(7, Reading(sensor));
            Assert.Equal(DeviceStatus.Critical, sensor.Status);
        }

        [Fact]
        public void TryRemoveStock_UnknownItem_ReturnsFalseWithMessage()
        {
            using var items = new TempDataFile("MILK-1L|30");
            using var loop = LoopFile("92");
            var sensor = new ShelfStockSensor(loop.Path, items.Path);

            bool removed = sensor.TryRemoveStock("BREAD", 1, out string? error);

            Assert.False(removed);
            Assert.NotNull(error);
            Assert.Equal(30, Reading(sensor));
        }

        [Fact]
        public void TryRemoveStock_MoreThanAvailable_LeavesTheShelfUntouched()
        {
            using var items = new TempDataFile("MILK-1L|5");
            using var loop = LoopFile("92");
            var sensor = new ShelfStockSensor(loop.Path, items.Path);

            bool removed = sensor.TryRemoveStock("MILK-1L", 6, out string? error);

            Assert.False(removed);
            Assert.NotNull(error);
            Assert.Equal(5, Reading(sensor));
        }

        [Fact]
        public void TryRemoveStock_NonPositiveQuantity_ReturnsFalse()
        {
            using var items = new TempDataFile("MILK-1L|30");
            using var loop = LoopFile("92");
            var sensor = new ShelfStockSensor(loop.Path, items.Path);

            Assert.False(sensor.TryRemoveStock("MILK-1L", 0, out _));
            Assert.False(sensor.TryRemoveStock("MILK-1L", -1, out _));
            Assert.Equal(30, Reading(sensor));
        }

        [Fact]
        public void EmptyShelf_IsCriticalAndStillAvailable()
        {
            using var items = new TempDataFile("MILK-1L|0");
            using var loop = LoopFile("92");
            var sensor = new ShelfStockSensor(loop.Path, items.Path);

            Assert.Equal(0, Reading(sensor));
            Assert.Equal(DeviceStatus.Critical, sensor.Status);
            Assert.True(sensor.IsDataAvailable);
        }

        [Fact]
        public void ReadData_WithItemInventory_StaysAvailable()
        {
            using var items = new TempDataFile("MILK-1L|30");
            using var loop = LoopFile("92");
            var sensor = new ShelfStockSensor(loop.Path, items.Path);

            sensor.ReadData();

            Assert.Equal(30, Reading(sensor));
            Assert.True(sensor.IsDataAvailable);
        }

        [Fact]
        public void WithoutItemInventory_LoopFilePublishesTheWholeCount()
        {
            using var loop = LoopFile("92");
            var sensor = new ShelfStockSensor(loop.Path);

            sensor.ReadData();

            Assert.False(sensor.HasItemInventory);
            Assert.Equal(92, Reading(sensor));
            Assert.Equal(DeviceStatus.Normal, sensor.Status);
        }

        [Fact]
        public void WithoutItemInventory_LowCount_IsCritical()
        {
            using var loop = LoopFile("9");
            var sensor = new ShelfStockSensor(loop.Path);

            sensor.ReadData();

            Assert.Equal(9, Reading(sensor));
            Assert.Equal(DeviceStatus.Critical, sensor.Status);
        }

        [Fact]
        public void WithoutItemInventory_FractionalCount_IsRejected()
        {
            using var loop = LoopFile("73.5");
            var sensor = new ShelfStockSensor(loop.Path);

            sensor.ReadData();

            Assert.False(sensor.IsDataAvailable);
            Assert.Null(sensor.Value);
        }

        [Fact]
        public void WithoutItemInventory_OverCapacity_IsRejected()
        {
            using var loop = LoopFile("101");
            var sensor = new ShelfStockSensor(loop.Path);

            sensor.ReadData();

            Assert.False(sensor.IsDataAvailable);
            Assert.Null(sensor.Value);
        }

        [Fact]
        public void MissingLoopFile_StaysUnavailableWithoutThrowing()
        {
            var sensor = new ShelfStockSensor(TempDataFile.MissingPath());

            sensor.ReadData();

            Assert.False(sensor.IsDataAvailable);
            Assert.Null(sensor.Value);
            Assert.NotNull(sensor.LastSimulationError);
        }

        [Fact]
        public void BrokenItemFile_FallsBackToTheLoopFile()
        {
            using var loop = LoopFile("92");
            var sensor = new ShelfStockSensor(loop.Path, TempDataFile.MissingPath());

            sensor.ReadData();

            Assert.False(sensor.HasItemInventory);
            Assert.Equal(92, Reading(sensor));
            Assert.True(sensor.IsDataAvailable);
        }
    }
}
