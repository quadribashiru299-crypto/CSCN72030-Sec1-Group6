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
        private static void printCommandRequest(CommandRequest cmd)
        {
            Console.WriteLine("Printing CommandRequest:");
            Console.WriteLine($"Command request for Device #{cmd.DeviceId}" +
                $"\nRequested State: {cmd.RequestedState}" +
                $"\nAdmin User ID: {cmd.AdminUserId}" +
                $"\nTimestamp: {cmd.Timestamp:HH:mm:ss}"
                );
            Console.WriteLine("---------------------\n");
        }
        private static void printCommandResult(CommandResult res)
        {
            Console.WriteLine("Printing CommandResult:");
            Console.Write($"Command request was success: ");
            if (res.IsSuccess)
            {
                Console.Write("YES\n");
            }
            else
            {
                Console.Write("NO\n");
            }
            Console.WriteLine($"Confirmed State: {res.ConfirmedState}");
            if (res.ErrorMessage != null) {
                Console.WriteLine($"Error message: {res.ErrorMessage}");
            }
            Console.WriteLine("---------------------\n");
            
        }

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

            Console.WriteLine("Reading telemetry from SmokeDetector...\n");
            for (int i = 1; i <= 4; i++)
            {
                Console.WriteLine($"--- Reading #{i} ---");

                smokeDetector.ReadData();
                TelemetryPayload smokeTelemetry = smokeDetector.GetTelemetry();

                // publishing read data to the message bus
                messageBus.Publish(smokeTelemetry);
            }
            Console.WriteLine("Telemetry reading from SmokeDetector finished.\n");

            Console.WriteLine("Reading initial telemetry from AlarmController...\n");
            alarmController.ReadData();
            TelemetryPayload alarmTelemetry = alarmController.GetTelemetry();
            messageBus.Publish(alarmTelemetry);
            Console.WriteLine("Reading initial telemetry from AlarmController finished.\n");

            Console.WriteLine("Simulating sending a CommandRequest to AlarmController...\n");
            CommandRequest cmd = new(alarmController.DeviceId, "Inactive", "this_is_admin_fr");
            CommandResult res = alarmController.SetConfig(cmd);
            Console.WriteLine("CommandRequest sent.\n");
            printCommandRequest(cmd);
            Console.WriteLine("CommandResult received.\n");
            printCommandResult(res);
            //Console.Write("\n");
            messageBus.Publish(alarmController.GetTelemetry());

            Console.WriteLine("Simulating sending a CommandRequest to AlarmController...\n");
            cmd = new(alarmController.DeviceId, "Active", "this_is_admin_fr");
            res = alarmController.SetConfig(cmd);
            Console.WriteLine("CommandRequest sent.\n");
            printCommandRequest(cmd);
            Console.WriteLine("CommandResult received.\n");
            printCommandResult(res);
            //Console.Write("\n");
            messageBus.Publish(alarmController.GetTelemetry());

            Console.WriteLine("\n==================================================");
            Console.WriteLine("       SmartShop Demo finished successfully       ");
            Console.WriteLine("==================================================\n");

            Console.ReadKey();
        }
    }
}