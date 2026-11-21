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
        public abstract TelemetryPayload GetTelemetry();

    }
}
