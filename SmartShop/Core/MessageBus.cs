using SmartShop.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SmartShop.Core
{
    internal class MessageBus
    {
        private readonly List<Action<TelemetryPayload>> Subscribers = new List<Action<TelemetryPayload>>();

        public void Subscribe(Action<TelemetryPayload> getMessageMethod)
        {
            Subscribers.Add(getMessageMethod);
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
