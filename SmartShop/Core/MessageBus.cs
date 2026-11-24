using SmartShop.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartShop.Core
{
    internal class MessageBus
    {
        private readonly List<Action<TelemetryPayload>> Subscribers = new List<Action<TelemetryPayload>>();

        public void Subscribe(Action<TelemetryPayload> getMessageMethod, string subsriberName)
        {
            Subscribers.Add(getMessageMethod);
            Console.WriteLine($"Subscriber {subsriberName} added.");
        }

        public void Publish(TelemetryPayload payload)
        {
            foreach (Action<TelemetryPayload> getMessageMethod in Subscribers)
            {
                getMessageMethod(payload);
            }
        }
    }
}
