using System;
using SmartShop.Core;
using SmartShop.Devices;
using SmartShop.Models;

namespace SmartShop
{
    internal class Program
    {
        static void Main(string[] args)
        {
            MessageBus bus = new MessageBus();

            MessageReceiver receiver = new MessageReceiver();

            //message receiver subscribes to message bus
            bus.Subscribe(receiver.ReceiveTelemetry);

            MockTempSensor sensor = new MockTempSensor
            {
                DeviceId = 1,
                Name = "Store Temperature Sensor",
                Unit = "°C",
                Bus = bus
            };

            Console.WriteLine("--- Starting SmartShop Message Bus Demo ---\n");

            //normal reading
            sensor.SimulateReading(21.5);

            //warning
            sensor.SimulateReading(27.8);

            //data unavailable
            sensor.SimulateFailure();

            Console.WriteLine("\n--- Demo Finished Successfully ---");
        }
    }
}