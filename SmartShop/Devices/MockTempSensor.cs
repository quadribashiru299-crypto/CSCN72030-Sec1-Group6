using System;
using System.Collections.Generic;
using System.Text;
using SmartShop.Core;
using SmartShop.Models;

namespace SmartShop.Devices
{
    internal class MockTempSensor
    {
        public int DeviceId {  get; set; }
        public string Name { get; set;  }
        public DeviceStatus Status { get; set; }
        public DateTime LastUpdated { get; set; }
        public bool IsDataAvailable { get; set; }
        public double LastKnownValue { get; set; }
        public string Unit {  get; set; }
        public MessageBus Bus { get; set; }

        public void SimulateReading(double value)
        {
            LastKnownValue = value;

            //Threshold simulation
            DeviceStatus status;
            if(value > 25.0){
                status = DeviceStatus.Warning;
            } else {
                status = DeviceStatus.Normal;
            }

            TelemetryPayload payload = new TelemetryPayload(DeviceId, value, Unit, status);

            Bus.Publish(payload);
        }

        public void SimulateFailure()
        {
            TelemetryPayload payload = new TelemetryPayload(
                DeviceId,
                0.0,
                Unit,
                DeviceStatus.Warning,
                false
            );

            Bus.Publish(payload);
        }
    }
}
