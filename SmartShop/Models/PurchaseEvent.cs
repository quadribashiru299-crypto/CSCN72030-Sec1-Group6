using System;

namespace SmartShop.Models
{
    /// <summary>
    /// Simulated kiosk purchase sent to ShelfStockSensor and CheckoutSensor (REQ-INT-KSK-001).
    /// No customer, cart or payment data (REQ-KSK-001).
    /// </summary>
    public class PurchaseEvent
    {
        public string ItemId { get; init; }

        /// <summary>Change in shelf quantity; negative for a purchase (e.g. -1).</summary>
        public int QuantityDelta { get; init; }

        public int StationId { get; init; }
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;

        public PurchaseEvent(string itemId, int quantityDelta, int stationId)
        {
            ItemId = itemId;
            QuantityDelta = quantityDelta;
            StationId = stationId;
        }
    }

    /// <summary>Outcome shown on the kiosk screen after a simulated purchase (REQ-USA-KSK-001).</summary>
    public class KioskPurchaseResult
    {
        public bool IsSuccess { get; init; }
        public string Message { get; init; }
        public PurchaseEvent? Event { get; init; }

        /// <summary>Telemetry published by CheckoutSensor after the purchase, if it succeeded.</summary>
        public TelemetryPayload? CheckoutTelemetry { get; init; }

        public KioskPurchaseResult(bool isSuccess, string message, PurchaseEvent? purchaseEvent = null,
                                   TelemetryPayload? checkoutTelemetry = null)
        {
            IsSuccess = isSuccess;
            Message = message;
            Event = purchaseEvent;
            CheckoutTelemetry = checkoutTelemetry;
        }
    }
}
