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
            if(this.Value == null
                || this.Value >= 45.0
                || this.Value <= -5.0)
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

        public StoreTemperatureSensor(int id, string name) {
            ThresholdConfig storeTempSensorConfig = new ThresholdConfig{ LowCritical = 16.0, 
                                                                         LowWarning = 19.0,
                                                                         HighWarning = 24.0,
                                                                         HighCritical = 27.0 };
            this.DeviceId = id;
            this.DeviceName = name;
            this.Unit = "°C";
            this.Thresholds = storeTempSensorConfig;
        }

        public override void ReadData()
        {
            //placeholder for readData method
        }

    }
}
