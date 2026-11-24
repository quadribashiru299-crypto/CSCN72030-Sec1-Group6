using System;
using System.Collections.Generic;
using System.Text;
using SmartShop.Core;
using SmartShop.Devices;
using SmartShop.Models;

namespace SmartShop
{
    public class Demo
    {
        static void Main()
        {
            Console.WriteLine("==================================================");
            Console.WriteLine("                 SmartShop Demo                    ");
            Console.WriteLine("==================================================\n");

            //inicializing a message bus and a message receiver
            Console.WriteLine("Inicializing a message bus and a message receiver...");
            MessageBus messageBus = new();
            MessageReceiver messageReceiver = new();
            Console.WriteLine($"Message bus and message receiver, {messageReceiver.name}, initialized.\n");

            Console.WriteLine($"Adding {messageReceiver.name} to the subscriber list of the message bus...");
            messageBus.Subscribe(messageReceiver.ReceiveTelemetry, messageReceiver.name);
            Console.WriteLine($"{messageReceiver.name} is now subscribing to the message bus.\n");

            Console.WriteLine("Inicialing simulated devices...");
            SmokeDetector smokeDetector = new SmokeDetector("Data/smoke_detector.txt");
            AlarmController alarmController = new AlarmController("Data/alarm_controller.txt");
            Console.WriteLine("Simulated devices inicialized.\n");

            Console.WriteLine("Reading telemetry from SmokeDetector...");
            for (int i = 1; i <= 4; i++)
            {
                Console.WriteLine($"\n--- Reading #{i} ---");

                smokeDetector.ReadData();
                TelemetryPayload smokeTelemetry = smokeDetector.GetTelemetry();

                // publishing read data to the message bus
                messageBus.Publish(smokeTelemetry);
            }


        }
    }
}
