using System;
using System.Collections.Generic;
using System.Text;
using SmartShop.Models;

namespace SmartShop.Core
{
    public abstract class Device
    {
        public int DeviceId { get; init; }
        public string DeviceName { get; init; }
        public DateTime LastUpdated { get; set; }
        public DeviceStatus Status { get; set; }
        public bool IsDataAvailable { get; set; }

        public virtual void ReadData() { }

        /// <summary>
        /// Packages the device into the shared telemetry contract (REQ-INT-TEL-001).
        /// Base devices carry no measured value; sensors override this to publish
        /// their reading and unit.
        /// </summary>
        public virtual TelemetryPayload GetTelemetry()
        {
            return new TelemetryPayload(DeviceId, null, string.Empty, Status, IsDataAvailable)
            {
                Timestamp = LastUpdated == default ? DateTime.UtcNow : LastUpdated
            };
        }
    }
}
