using System;
using System.Collections.Generic;
using System.Text;

//these are only here because there had been no merges so far
//they are defined in the TelemetryPaload.cs file
//this file will need to include "using SmartShop.Models"
public struct CommandRequest;
public struct CommandResult;

namespace SmartShop.Core
{
    public struct DeviceConfig
    {
        public string State { get; set; } // e.g., "Locked", "On", "Auto", "Active"
        public int? Value { get; set; }   //optional percentage number (e.g.,for brightness or fan speed), otherwise null
    }
    public abstract class Controller : Device
    {
        public DeviceConfig CurrentConfig { get; set; }

        public abstract CommandResult SetConfig(CommandRequest cmd);
    }
}
