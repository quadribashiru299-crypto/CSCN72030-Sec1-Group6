using System;
using System.Collections.Generic;
using System.Text;
using SmartShop.Models;

namespace SmartShop.Core
{
    public abstract class Device
    {
        public int DeviceId { get; init; }
        public string DeviceName { get; init; } = string.Empty;
        public DateTime LastUpdated { get; set; }
        public DeviceStatus Status { get; set; }
        public bool IsDataAvailable { get; set; }

        public virtual void ReadData() { }

        /// <summary>
        /// Packages the device into the shared telemetry contract (REQ-INT-TEL-001).
        /// Every concrete device declares its own payload, so the compiler forces
        /// each subclass to state exactly what it publishes.
        /// </summary>
        public abstract TelemetryPayload GetTelemetry();
    }
}
