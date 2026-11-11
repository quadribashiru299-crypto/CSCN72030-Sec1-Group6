using SmartShop.Core;
using System;
using System.Collections.Generic;
using System.Text;
using SmartShop;
using SmartShop.Models;

namespace SmartShop
{
    internal class MessageReceiver
    {
        public TelemetryPayload LastReceivedTelemetryTempSensor { get; set; }
        public MessageReceiver()
        {
            LastReceivedTelemetryTempSensor = null;
        }

        //Tahle funkce se volá z MessageBus jenom
        public void ReceiveTelemetry(TelemetryPayload payload)
        {
            LastReceivedTelemetryTempSensor = payload;

            if (LastReceivedTelemetryTempSensor == null) {
                Console.WriteLine("Telemetry message not found.");
                return;
            }

            //print telemetry payload
            Console.WriteLine($"[HMI Receiver] Device #{payload.DeviceId} | " +
                              $"Value: {payload.Value} {payload.Unit} | " +
                              $"Status: {payload.Status} | " +
                              $"Available: {payload.IsDataAvailable} | " +
                              $"Timestamp: {payload.Timestamp:HH:mm:ss}");
        }

    }
}
