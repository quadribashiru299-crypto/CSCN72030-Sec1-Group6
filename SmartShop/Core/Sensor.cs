using System;
using System.Collections.Generic;
using System.Text;

namespace SmartShop.Core
{
    public struct ThresholdConfig
    {
        public double? LowCritical;
        public double? LowWarning;
        public double? HighWarning;
        public double? HighCritical;
    }

    public abstract class Sensor : Device
    {
        public double? Value { get; set; }
        public string Unit { get; set; }
        public ThresholdConfig Thresholds { get; set; }

        //Checks if the received data makes sense and sets the IsDataAvailable attribute
        public abstract bool Validate();
         
        //Compares the read value with configured and returns resulting status
        public DeviceStatus ClassifyStatus()
        {
            if (!IsDataAvailable || Value == null)
            {
                Status = DeviceStatus.Warning;
                return DeviceStatus.Warning;
            }
            if((Thresholds.LowCritical != null && Value <= Thresholds.LowCritical) || (Thresholds.HighCritical != null && Value >= Thresholds.HighCritical))
            {
                Status = DeviceStatus.Critical;
                return DeviceStatus.Critical;
            }
            if((Thresholds.LowWarning != null && Value <= Thresholds.LowWarning) || (Thresholds.HighWarning != null && Value >= Thresholds.HighWarning))
            {
                Status = DeviceStatus.Warning;
                return DeviceStatus.Warning;
            }
            Status = DeviceStatus.Normal;
            return DeviceStatus.Normal;
        }
    }
}
