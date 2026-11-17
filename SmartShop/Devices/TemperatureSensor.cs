using SmartShop.Core;
using SmartShop.Models;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace SmartShop.Devices
{
    public class TemperatureSensor : Sensor
    {
        public override bool Validate()
        {
            return true;
        }
        public override TelemetryPayload GetTelemetry()
        {
            TelemetryPayload result = new TelemetryPayload(this.DeviceId, this.Value, this.Unit, this.Status );
            return result;
        }

    }
}
