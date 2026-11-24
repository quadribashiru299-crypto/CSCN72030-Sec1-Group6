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
        public string name;
        public MessageReceiver(string name = "Bob the Message Receiver")
        {
            this.name = name;
        }
        
        public void printTelemetry(TelemetryPayload payload)
        {
            Console.WriteLine("Printing telemetry:");
            Console.WriteLine($"[HMI Receiver] Device #{payload.DeviceId}" +
                $"\nValue: {payload.Value} {payload.Unit}" +
                $"\nStatus: {payload.Status}" +
                $"\nAvailable: {payload.IsDataAvailable}" +
                $"\nTimestamp: {payload.Timestamp:HH:mm:ss}"
                );
            Console.WriteLine("---------------------\n");

        }

        //this function is given to the message bus and is called only from the message bus
        public void ReceiveTelemetry(TelemetryPayload payload)
        {
            if (payload == null) {
                Console.WriteLine("Telemetry message not found.");
                return;
            }

            Console.WriteLine("Telemetry received.");
            printTelemetry(payload);

            ////print telemetry payload
            //Console.WriteLine($"[HMI Receiver] Device #{payload.DeviceId} | " +
            //                  $"Value: {payload.Value} {payload.Unit} | " +
            //                  $"Status: {payload.Status} | " +
            //                  $"Available: {payload.IsDataAvailable} | " +
            //                  $"Timestamp: {payload.Timestamp:HH:mm:ss}");
        }

    }
}