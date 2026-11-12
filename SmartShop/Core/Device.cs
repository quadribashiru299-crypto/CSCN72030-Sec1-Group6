using System;
using System.Collections.Generic;
using System.Text;
//using SmartShop.Models;


//these are only here because there had been no merges so far
//they are defined in the TelemetryPaload.cs file
//this file will need to include "using SmartShop.Models"
public enum DeviceStatus
{
    Normal,
    Warning,
    Critical
}
public struct TelemetryPayload { };

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
        public abstract TelemetryPayload GetTelemetry();

    }
}
