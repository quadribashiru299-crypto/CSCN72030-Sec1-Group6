using SmartShop.Core;
using SmartShop.Models;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace SmartShop.Devices
{
    public class StoreTemperatureSensor : Sensor
    {
        public override bool Validate()
        {
            // adding all the conditions for unvalid results
            // null
            // max value that makes sense
            // min value that makes sense
            if(this.Value == null)
            {
                return false;
            }
            return true;
        }
        public override TelemetryPayload GetTelemetry()
        {
            TelemetryPayload result = new TelemetryPayload(this.DeviceId, this.Value, this.Unit, this.Status);
            return result;
        }

        public StoreTemperatureSensor(int id, string name, ThresholdConfig thresholds) {
                
        }
    }
}
