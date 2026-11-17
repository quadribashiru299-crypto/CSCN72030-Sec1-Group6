namespace SmartShop.Core
{
    /// <summary>
    /// What the kiosk needs from ShelfStockSensor (REQ-KSK-002). ShelfStockSensor
    /// implements this so the kiosk does not depend on its concrete class.
    /// </summary>
    public interface IShelfStock
    {
        /// <summary>
        /// Removes <paramref name="quantity"/> units of an item from the shelf.
        /// Returns false and an error message (e.g. unknown item, not enough stock)
        /// without changing stock if the purchase cannot be applied.
        /// </summary>
        bool TryRemoveStock(string itemId, int quantity, out string? errorMessage);
    }
}
