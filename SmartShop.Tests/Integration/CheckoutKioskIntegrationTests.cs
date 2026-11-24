using SmartShop.Core;
using SmartShop.Devices;
using SmartShop.Models;
using SmartShop.Services;

namespace SmartShop.Tests.Integration
{
    /// <summary>
    /// M2 integration tests for the checkout area (Bashiru): KioskPurchaseSimulator,
    /// CheckoutSensor and CheckoutStationController working together, driven by the
    /// real looped simulation files shipped in SmartShop/Data.
    /// Test IDs match docs/test-plan-checkout-kiosk.md.
    /// </summary>
    public class CheckoutKioskIntegrationTests
    {
        private const string Admin = "admin-01";

        /// <summary>
        /// In-memory shelf used until ShelfStockSensor (Weiming) is merged to main;
        /// it follows the same IShelfStock contract the real sensor implements.
        /// </summary>
        private sealed class InMemoryShelf : IShelfStock
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

        /// <summary>Path of a simulation file copied to the test output from SmartShop/Data.</summary>
        private static string ShippedDataFile(string fileName)
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Data", fileName);
            Assert.True(File.Exists(path), $"Shipped simulation file not found at '{path}'.");
            return path;
        }

        private static int RecordCount(string path) =>
            File.ReadAllLines(path)
                .Select(line => line.Trim())
                .Count(line => line.Length > 0 && !line.StartsWith('#'));

        // IT-CHK-01
        [Fact]
        public void ShippedCheckoutSensorFile_EveryRecordIsValid_AndCoversAllStatusBands()
        {
            string path = ShippedDataFile("checkout_sensor.txt");
            var sensor = new CheckoutSensor(path);
            var statuses = new HashSet<DeviceStatus>();

            int records = RecordCount(path);
            Assert.True(records > 0);

            for (int i = 0; i < records; i++)
            {
                sensor.ReadData();
                Assert.True(sensor.IsDataAvailable, $"Record {i + 1}: {sensor.LastReadError}");
                statuses.Add(sensor.Status);
            }

            Assert.Contains(DeviceStatus.Normal, statuses);
            Assert.Contains(DeviceStatus.Warning, statuses);
            Assert.Contains(DeviceStatus.Critical, statuses);
        }

        // IT-CHK-02
        [Fact]
        public void ShippedCheckoutSensorFile_LoopsBackToFirstRecord()
        {
            string path = ShippedDataFile("checkout_sensor.txt");
            var sensor = new CheckoutSensor(path);

            sensor.ReadData();
            double? first = sensor.Value;
            for (int i = 1; i < RecordCount(path); i++)
            {
                sensor.ReadData();
            }
            sensor.ReadData(); // wraps

            Assert.Equal(first, sensor.Value);
            Assert.True(sensor.IsDataAvailable);
        }

        // IT-CSC-01
        [Fact]
        public void ShippedCheckoutStationFile_EveryRecordIsValid_AndHasBothLaneStates()
        {
            string path = ShippedDataFile("checkout_station.txt");
            var controller = new CheckoutStationController(path);
            var states = new HashSet<string>();

            int records = RecordCount(path);
            Assert.True(records > 0);

            for (int i = 0; i < records; i++)
            {
                controller.ReadData();
                Assert.True(controller.IsDataAvailable, $"Record {i + 1}: {controller.LastReadError}");
                states.Add(controller.GetConfig().State);
            }

            Assert.Contains(CheckoutStationController.StateOpen, states);
            Assert.Contains(CheckoutStationController.StateClosed, states);
        }

        // IT-KSK-01
        [Fact]
        public void KioskPurchase_ReducesShelfStock_AndRaisesCheckoutActivity()
        {
            using var file = new TempDataFile("CheckoutSensor|5|count");
            var sensor = new CheckoutSensor(file.Path);
            var shelf = new InMemoryShelf();
            shelf.Stock["MILK-1L"] = 3;
            var kiosk = new KioskPurchaseSimulator(sensor, shelf);

            sensor.ReadData(); // 5 customers: Normal

            KioskPurchaseResult result = kiosk.SimulatePurchase("MILK-1L");

            Assert.True(result.IsSuccess, result.Message);
            Assert.Equal(2, shelf.Stock["MILK-1L"]);
            Assert.Equal(6, sensor.Value);
            Assert.Equal(1, sensor.TotalTransactions);
            Assert.NotNull(result.CheckoutTelemetry);
            Assert.Equal(6, result.CheckoutTelemetry!.Value);
            Assert.Equal(DeviceStatus.Warning, result.CheckoutTelemetry.Status); // crossed HighWarning (6)
        }

        // IT-KSK-02
        [Fact]
        public void KioskPurchase_OutOfStock_ChangesNeitherShelfNorCheckout()
        {
            using var file = new TempDataFile("CheckoutSensor|2|count");
            var sensor = new CheckoutSensor(file.Path);
            var shelf = new InMemoryShelf();
            shelf.Stock["MILK-1L"] = 1;
            var kiosk = new KioskPurchaseSimulator(sensor, shelf);
            var logged = new List<PurchaseEvent>();
            kiosk.PurchaseCompleted += logged.Add;
            sensor.ReadData();

            KioskPurchaseResult result = kiosk.SimulatePurchase("MILK-1L", quantity: 2);

            Assert.False(result.IsSuccess);
            Assert.Contains("Purchase not completed", result.Message);
            Assert.Equal(1, shelf.Stock["MILK-1L"]);
            Assert.Equal(2, sensor.Value);
            Assert.Equal(0, sensor.TotalTransactions);
            Assert.Empty(logged);
        }

        // IT-KSK-03
        [Fact]
        public void KioskPurchase_RaisesPurchaseCompleted_ForLogging()
        {
            var sensor = new CheckoutSensor(TempDataFile.MissingPath());
            var shelf = new InMemoryShelf();
            shelf.Stock["BREAD"] = 10;
            var kiosk = new KioskPurchaseSimulator(sensor, shelf, stationId: 2);
            var logged = new List<PurchaseEvent>();
            kiosk.PurchaseCompleted += logged.Add;

            kiosk.SimulatePurchase("BREAD", quantity: 3);

            PurchaseEvent purchase = Assert.Single(logged);
            Assert.Equal("BREAD", purchase.ItemId);
            Assert.Equal(-3, purchase.QuantityDelta);
            Assert.Equal(2, purchase.StationId);
        }

        // IT-KSK-04
        [Fact]
        public void KioskPurchase_ThenNextSimulationReading_ResumesLoopedValue()
        {
            using var file = new TempDataFile("CheckoutSensor|3|count", "CheckoutSensor|1|count");
            var sensor = new CheckoutSensor(file.Path);
            var shelf = new InMemoryShelf();
            shelf.Stock["EGGS"] = 5;
            var kiosk = new KioskPurchaseSimulator(sensor, shelf);

            sensor.ReadData();              // 3
            kiosk.SimulatePurchase("EGGS"); // 4
            Assert.Equal(4, sensor.Value);

            sensor.ReadData();              // next looped record replaces it
            Assert.Equal(1, sensor.Value);
            Assert.Equal(1, sensor.TotalTransactions); // purchase count is kept
        }

        // IT-KSK-05
        [Fact]
        public void KioskPurchase_WhileSensorFileMissing_StillRecordsActivity()
        {
            var sensor = new CheckoutSensor(TempDataFile.MissingPath());
            var shelf = new InMemoryShelf();
            shelf.Stock["MILK-1L"] = 1;
            var kiosk = new KioskPurchaseSimulator(sensor, shelf);
            sensor.ReadData(); // unavailable, no crash

            KioskPurchaseResult result = kiosk.SimulatePurchase("MILK-1L");

            Assert.True(result.IsSuccess);
            Assert.True(sensor.IsDataAvailable);
            Assert.Equal(1, sensor.Value);
        }

        // IT-CSC-02
        [Fact]
        public void LaneCommand_WithoutAdmin_IsRejected_AndTelemetryStillShowsOpen()
        {
            using var file = new TempDataFile("CheckoutStationController|1|bool");
            var controller = new CheckoutStationController(file.Path);
            controller.ReadData();

            CommandResult result = controller.SetConfig(
                new CommandRequest(controller.DeviceId, "Closed", adminUserId: ""));

            Assert.False(result.IsSuccess);
            Assert.Equal(1, controller.GetTelemetry().Value);
            Assert.False(controller.IsManualOverride);
        }

        // IT-CSC-03
        [Fact]
        public void LaneCommand_ByAdmin_OverridesSimulation_UntilResumed()
        {
            using var file = new TempDataFile("CheckoutStationController|1|bool");
            var controller = new CheckoutStationController(file.Path);
            controller.ReadData(); // Open from file

            CommandResult result = controller.SetConfig(
                new CommandRequest(controller.DeviceId, "Closed", Admin));
            controller.ReadData(); // file says Open, override wins

            Assert.True(result.IsSuccess);
            Assert.Equal(CheckoutStationController.StateOpen, controller.LastPreviousState);
            Assert.Equal(0, controller.GetTelemetry().Value);

            controller.ResumeSimulation();
            controller.ReadData();

            Assert.Equal(1, controller.GetTelemetry().Value);
        }

        // IT-CSC-04
        [Fact]
        public void LaneCommand_DeviceFails_StateUnchanged_AndNoOverride()
        {
            var controller = new CheckoutStationController { SimulateCommandFailure = true };

            CommandResult result = controller.SetConfig(
                new CommandRequest(controller.DeviceId, "Closed", Admin));

            Assert.False(result.IsSuccess);
            Assert.Equal(CheckoutStationController.StateOpen, result.ConfirmedState);
            Assert.True(controller.IsLaneOpen);
            Assert.False(controller.IsManualOverride);
        }
    }
}
