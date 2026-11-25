using SmartShop.Core;
using SmartShop.Devices;
using SmartShop.Models;
using SmartShop.Services;

namespace SmartShop.Tests.Services
{
    public class KioskPurchaseSimulatorTests
    {
        /// <summary>In-memory stand-in for ShelfStockSensor.</summary>
        private sealed class FakeShelf : IShelfStock
        {
            public Dictionary<string, int> Stock { get; } = new(StringComparer.OrdinalIgnoreCase);

            public bool TryRemoveStock(string itemId, int quantity, out string? errorMessage)
            {
                if (!Stock.TryGetValue(itemId, out int available))
                {
                    errorMessage = $"Unknown item '{itemId}'.";
                    return false;
                }
                if (available < quantity)
                {
                    errorMessage = $"Only {available} left.";
                    return false;
                }
                Stock[itemId] = available - quantity;
                errorMessage = null;
                return true;
            }
        }

        private static (KioskPurchaseSimulator kiosk, CheckoutSensor sensor, FakeShelf shelf) Create(int milk = 5)
        {
            var sensor = new CheckoutSensor(TempDataFile.MissingPath());
            var shelf = new FakeShelf();
            shelf.Stock["MILK-1L"] = milk;
            return (new KioskPurchaseSimulator(sensor, shelf), sensor, shelf);
        }

        [Fact]
        public void SimulatePurchase_DecrementsStockAndAddsCheckoutActivity()
        {
            var (kiosk, sensor, shelf) = Create(milk: 5);

            KioskPurchaseResult result = kiosk.SimulatePurchase("MILK-1L");

            Assert.True(result.IsSuccess);
            Assert.Equal(4, shelf.Stock["MILK-1L"]);
            Assert.Equal(1, sensor.TotalTransactions);
            Assert.NotNull(result.CheckoutTelemetry);
            Assert.Equal(1, result.CheckoutTelemetry!.Value);
            Assert.Contains("ShelfStockSensor", result.Message);
            Assert.Contains("CheckoutSensor", result.Message);
        }

        [Fact]
        public void SimulatePurchase_BuildsPurchaseEventStructure()
        {
            var (kiosk, _, _) = Create();

            KioskPurchaseResult result = kiosk.SimulatePurchase("MILK-1L", 2);

            Assert.NotNull(result.Event);
            Assert.Equal("MILK-1L", result.Event!.ItemId);
            Assert.Equal(-2, result.Event.QuantityDelta);
            Assert.Equal(KioskPurchaseSimulator.DefaultStationId, result.Event.StationId);
        }

        [Fact]
        public void SimulatePurchase_RaisesPurchaseCompletedForLogging()
        {
            var (kiosk, _, _) = Create();
            PurchaseEvent? logged = null;
            kiosk.PurchaseCompleted += e => logged = e;

            kiosk.SimulatePurchase("MILK-1L");

            Assert.NotNull(logged);
            Assert.Equal("MILK-1L", logged!.ItemId);
        }

        [Fact]
        public void SimulatePurchase_OutOfStock_ChangesNothing()
        {
            var (kiosk, sensor, shelf) = Create(milk: 0);
            bool raised = false;
            kiosk.PurchaseCompleted += _ => raised = true;

            KioskPurchaseResult result = kiosk.SimulatePurchase("MILK-1L");

            Assert.False(result.IsSuccess);
            Assert.Contains("Only 0 left", result.Message);
            Assert.Equal(0, shelf.Stock["MILK-1L"]);
            Assert.Equal(0, sensor.TotalTransactions);
            Assert.False(raised);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void SimulatePurchase_NoItem_IsRejected(string itemId)
        {
            var (kiosk, sensor, _) = Create();

            KioskPurchaseResult result = kiosk.SimulatePurchase(itemId);

            Assert.False(result.IsSuccess);
            Assert.Equal(0, sensor.TotalTransactions);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-3)]
        public void SimulatePurchase_NonPositiveQuantity_IsRejected(int quantity)
        {
            var (kiosk, _, shelf) = Create(milk: 5);

            KioskPurchaseResult result = kiosk.SimulatePurchase("MILK-1L", quantity);

            Assert.False(result.IsSuccess);
            Assert.Equal(5, shelf.Stock["MILK-1L"]);
        }

        [Fact]
        public void SimulatePurchase_UnknownItem_IsRejected()
        {
            var (kiosk, _, _) = Create();

            KioskPurchaseResult result = kiosk.SimulatePurchase("BREAD");

            Assert.False(result.IsSuccess);
            Assert.Contains("Unknown item", result.Message);
        }

        [Fact]
        public void Constructor_NullDependencies_Throw()
        {
            var sensor = new CheckoutSensor(TempDataFile.MissingPath());
            var shelf = new FakeShelf();

            Assert.Throws<ArgumentNullException>(() => new KioskPurchaseSimulator(null!, shelf));
            Assert.Throws<ArgumentNullException>(() => new KioskPurchaseSimulator(sensor, null!));
        }
    }
}
