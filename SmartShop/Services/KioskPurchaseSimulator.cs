using System;
using SmartShop.Core;
using SmartShop.Devices;
using SmartShop.Models;

namespace SmartShop.Services
{
    /// <summary>
    /// Triggers simulated in-store purchases (REQ-KSK-001, REQ-KSK-002): each purchase
    /// removes stock through <see cref="IShelfStock"/> and adds checkout activity to
    /// <see cref="CheckoutSensor"/>. No customer accounts, carts or payments.
    /// SQLite logging is done by whoever subscribes to <see cref="PurchaseCompleted"/>
    /// (e.g. LoggingService), so this class never touches the database directly.
    /// </summary>
    public class KioskPurchaseSimulator
    {
        public const int DefaultStationId = 1;

        private readonly CheckoutSensor _checkoutSensor;
        private readonly IShelfStock _shelfStock;

        public int StationId { get; }

        /// <summary>Raised after every successful simulated purchase, for SQLite logging.</summary>
        public event Action<PurchaseEvent>? PurchaseCompleted;

        public KioskPurchaseSimulator(CheckoutSensor checkoutSensor, IShelfStock shelfStock,
                                      int stationId = DefaultStationId)
        {
            _checkoutSensor = checkoutSensor ?? throw new ArgumentNullException(nameof(checkoutSensor));
            _shelfStock = shelfStock ?? throw new ArgumentNullException(nameof(shelfStock));
            StationId = stationId;
        }

        /// <summary>
        /// Simulates buying <paramref name="quantity"/> units of an item in one click.
        /// If the shelf cannot supply the item, nothing is changed and the result explains why.
        /// </summary>
        public KioskPurchaseResult SimulatePurchase(string itemId, int quantity = 1)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return new KioskPurchaseResult(false, "Choose an item to purchase.");
            }
            if (quantity < 1)
            {
                return new KioskPurchaseResult(false, "Quantity must be at least 1.");
            }

            string item = itemId.Trim();
            if (!_shelfStock.TryRemoveStock(item, quantity, out string? stockError))
            {
                return new KioskPurchaseResult(false,
                    $"Purchase not completed: {stockError ?? "the shelf could not supply this item."}");
            }

            var purchase = new PurchaseEvent(item, -quantity, StationId);
            TelemetryPayload checkoutTelemetry = _checkoutSensor.RecordTransaction(purchase.Timestamp);

            PurchaseCompleted?.Invoke(purchase);

            string units = quantity == 1 ? "unit" : "units";
            return new KioskPurchaseResult(true,
                $"Purchased {quantity} {units} of {item} at station {StationId}. " +
                "Updated: ShelfStockSensor (stock reduced) and CheckoutSensor (checkout activity).",
                purchase,
                checkoutTelemetry);
        }
    }
}
