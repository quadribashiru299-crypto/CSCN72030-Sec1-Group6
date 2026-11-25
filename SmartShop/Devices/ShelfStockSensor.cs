using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SmartShop.Core;
using SmartShop.Models;

namespace SmartShop.Devices
{
    /// <summary>
    /// Reports how many items are on the shelf (REQ-TEL-007) and accepts stock
    /// removals from the kiosk (T24) through <see cref="IShelfStock"/>.
    ///
    /// Two data sources are supported:
    /// <list type="bullet">
    /// <item>an optional per-item file (<c>itemId|quantity</c>) which becomes the
    /// authoritative inventory; the published value is the sum of those counts and
    /// every successful removal updates it;</item>
    /// <item>when no per-item file is configured, the looped simulation file is
    /// used as a read-only total, exactly like the other threshold sensors.</item>
    /// </list>
    /// </summary>
    public sealed class ShelfStockSensor : Sensor, IShelfStock
    {
        public const string SensorKey = "ShelfStockSensor";
        public const int DefaultDeviceId = 4;
        public const string DefaultUnit = "count";

        public const double DefaultLowCritical = 10.0;
        public const double DefaultLowWarning = 20.0;

        /// <summary>One shelf section holds at most this many items.</summary>
        public const double MaxShelfCapacity = 100.0;

        private readonly Dictionary<string, int> _items =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Total units currently on the shelf.</summary>
        public int TotalItems
        {
            get
            {
                int total = 0;

                foreach (int quantity in _items.Values)
                {
                    total += quantity;
                }

                return total;
            }
        }

        /// <summary>Per-item baseline is in use (as opposed to the read-only file).</summary>
        public bool HasItemInventory => _items.Count > 0;

        public ShelfStockSensor(
            string dataFilePath,
            string? itemsFilePath = null,
            int deviceId = DefaultDeviceId,
            ThresholdConfig? thresholds = null)
        {
            if (dataFilePath is null)
            {
                throw new ArgumentNullException(nameof(dataFilePath));
            }

            SimulationFilePath = dataFilePath;
            DeviceId = deviceId;
            DeviceName = "Shelf Stock Level";
            Unit = DefaultUnit;
            Thresholds = thresholds ?? new ThresholdConfig
            {
                LowWarning = DefaultLowWarning,
                LowCritical = DefaultLowCritical
            };

            IsDataAvailable = false;
            Status = DeviceStatus.Warning;

            if (itemsFilePath is not null)
            {
                LoadItems(itemsFilePath);

                if (_items.Count > 0)
                {
                    RefreshFromItems();
                }
            }
        }

        /// <summary>
        /// Refreshes from the per-item inventory when one is loaded; otherwise the
        /// base class reads the next looped record.
        /// </summary>
        public override void ReadData()
        {
            if (_items.Count > 0)
            {
                RefreshFromItems();
                return;
            }

            base.ReadData();
        }

        /// <summary>
        /// Takes stock away for a completed sale. The change is applied only when
        /// the whole request can be satisfied, so a refused sale leaves the shelf
        /// untouched.
        /// </summary>
        public bool TryRemoveStock(string itemId, int quantity, out string? errorMessage)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                errorMessage = "Item id is required.";
                return false;
            }

            if (quantity <= 0)
            {
                errorMessage = "Quantity must be a positive number.";
                return false;
            }

            if (!_items.TryGetValue(itemId, out int available))
            {
                errorMessage = $"Unknown item '{itemId}'.";
                return false;
            }

            if (available < quantity)
            {
                errorMessage = $"Only {available} unit(s) of '{itemId}' left.";
                return false;
            }

            _items[itemId] = available - quantity;
            errorMessage = null;
            RefreshFromItems();
            return true;
        }

        /// <summary>
        /// Range check for the looped-file mode: a shelf count is a whole number
        /// between zero and the section capacity.
        /// </summary>
        public override bool Validate()
        {
            bool isValid = CandidateValue is double items
                && double.IsFinite(items)
                && items >= 0.0
                && items <= MaxShelfCapacity
                && items == Math.Floor(items);

            IsDataAvailable = isValid;
            return isValid;
        }

        private void RefreshFromItems()
        {
            Value = TotalItems;
            IsDataAvailable = true;
            LastUpdated = DateTime.UtcNow;
            ClassifyStatus();
        }

        private void LoadItems(string itemsFilePath)
        {
            _items.Clear();

            try
            {
                foreach (string rawLine in File.ReadAllLines(itemsFilePath))
                {
                    string line = rawLine.Trim();

                    if (line.Length == 0 || line.StartsWith('#'))
                    {
                        continue;
                    }

                    string[] fields = line.Split('|');

                    if (fields.Length != 2)
                    {
                        continue;
                    }

                    string itemId = fields[0].Trim();

                    if (itemId.Length == 0)
                    {
                        continue;
                    }

                    if (int.TryParse(
                        fields[1].Trim(),
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out int quantity) && quantity >= 0)
                    {
                        _items[itemId] = quantity;
                    }
                }
            }
            catch (Exception ex) when (ex is IOException
                                       || ex is UnauthorizedAccessException
                                       || ex is ArgumentException
                                       || ex is NotSupportedException)
            {
                // A broken inventory file must not crash the HMI (REQ-NFR-REL-001).
                _items.Clear();
            }
        }
    }
}
