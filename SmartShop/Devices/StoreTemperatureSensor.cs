using SmartShop.Core;
using SmartShop.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartShop.Devices
{
    public class StoreTemperatureSensor : Sensor
    {
        private readonly string filePath;
        public double? Humidity { get; set; } //we can leave this out as having two values for one sensor makes things bit more complicated

        public StoreTemperatureSensor(int id, string name, string filePath)
        {
            ThresholdConfig storeTempSensorConfig = new ThresholdConfig
            {
                LowCritical = 16.0,
                LowWarning = 19.0,
                HighWarning = 24.0,
                HighCritical = 27.0
            };
            this.DeviceId = id;
            this.DeviceName = name;
            this.Unit = "°C";
            this.Thresholds = storeTempSensorConfig;
            this.LastUpdated = DateTime.UtcNow;
            this.filePath = filePath;
        }

        public override bool Validate()
        {
            // adding all the conditions for unvalid results
            // null
            // max value that makes sense
            // min value that makes sense
            if(this.Value == null
                || this.Value >= 45.0
                || this.Value <= -5.0
                || !double.IsFinite(this.Value.Value))
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

        public override void ReadData()
        {
            //placeholder for readData method until we figure out the files
            //this sensor will need either two files or two sets of data in one file (temperature + humidity)
        }

    }
}
