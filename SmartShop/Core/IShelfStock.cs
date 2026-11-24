namespace SmartShop.Core
{
    /// <summary>
    /// Read/write access to the shelf inventory, used by the kiosk purchase
    /// simulator (T24) to take stock away when a sale happens.
    /// </summary>
    public interface IShelfStock
    {
        /// <summary>
        /// Removes <paramref name="quantity"/> units of <paramref name="itemId"/>.
        /// Returns false and fills <paramref name="errorMessage"/> when the item is
        /// unknown, the quantity is not positive, or there is not enough stock.
        /// </summary>
        bool TryRemoveStock(string itemId, int quantity, out string? errorMessage);
    }
}
