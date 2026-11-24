//using System;
//using SmartShop.Core;
//using SmartShop.Devices;
//using SmartShop.Models;
//
//namespace SmartShop
//{
//    internal class Program
//    {
//        static void Main(string[] args)
//        {
//            Console.WriteLine("==================================================");
//            Console.WriteLine("   SmartShop SCADA - Message Bus Integration Demo ");
//            Console.WriteLine("==================================================\n");
//
//            // inicialization of the message bus
//            MessageBus bus = new MessageBus();
//
//            // 2. Registrace odběratelů (vzor Observer / Pub-Sub)
//            // Odběratel 1: Tvůj MessageReceiver (simulace HMI Dashboardu)
//            MessageReceiver hmiReceiver = new MessageReceiver();
//            bus.Subscribe(hmiReceiver.ReceiveTelemetry);
//
//            // Odběratel 2: Ukázka druhého odběratele (simulace Alert Loggeru pro varování/výpadky)
//            bus.Subscribe(payload =>
//            {
//                if (!payload.IsDataAvailable || payload.Status != DeviceStatus.Normal)
//                {
//                    Console.WriteLine($"   >>> [ALERT LOG] Device Event #{payload.DeviceId}: Status={payload.Status}, Available={payload.IsDataAvailable}");
//                }
//            });
//
//            // 3. Vytvoření provizorních senzorů napojených na Message Bus
//            MockTempSensor storeSensor = new MockTempSensor
//            {
//                DeviceId = 1,
//                Name = "Store Temperature Sensor",
//                Unit = "°C",
//                Bus = bus
//            };
//
//            MockTempSensor fridgeSensor = new MockTempSensor
//            {
//                DeviceId = 2,
//                Name = "Refrigerator Temperature Sensor",
//                Unit = "°C",
//                Bus = bus
//            };
//
//            // 4. Průběh simulace
//            Console.WriteLine("[Step 1] Normal Reading:");
//            storeSensor.SimulateReading(21.5);
//            fridgeSensor.SimulateReading(4.2);
//
//            Console.WriteLine("\n[Step 2] Value Over Limit:");
//            storeSensor.SimulateReading(27.8);
//
//            Console.WriteLine("\n[Step 3] Data Unavailable:");
//            fridgeSensor.SimulateFailure();
//
//            // 5. Ověření, že si Receiver uložil poslední přijatou zprávu
//            Console.WriteLine("\n[Step 4] Checking The Last Message Received:");
//            if (hmiReceiver.LastReceivedTelemetryTempSensor != null)
//            {
//                var last = hmiReceiver.LastReceivedTelemetryTempSensor;
//                Console.WriteLine($"Last Saved Log -> Device #{last.DeviceId} | Value: {last.Value} {last.Unit} | Available: {last.IsDataAvailable}");
//            }
//
//            Console.WriteLine("\n==================================================");
//            Console.WriteLine("             Demo success!                        ");
//            Console.WriteLine("==================================================");
//        }
//    }
//}
//
//

using System;
using SmartShop.Core;
using SmartShop.Devices;
using SmartShop.Models;

namespace SmartShop
{
    internal class Program
    {
        public void Main(string[] args)
        {
            MessageBus bus = new MessageBus();

            MessageReceiver receiver = new MessageReceiver();

            //message receiver subscribes to message bus
            bus.Subscribe(receiver.ReceiveTelemetry, receiver.name);

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