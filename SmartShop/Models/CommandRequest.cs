using System;

namespace SmartShop.Models
{
    /// <summary>
    /// Control command sent from HMI to an actuator (REQ-INT-CTL-001)
    /// </summary>
    public class CommandRequest
    {
        public int DeviceId { get; init; }
        public string RequestedState { get; init; }
        public string AdminUserId { get; init; }
        public DateTime Timestamp { get; init; } = DateTime.Now;

        public CommandRequest(int deviceId, string requestedState, string adminUserId)
        {
            DeviceId = deviceId;
            RequestedState = requestedState;
            AdminUserId = adminUserId;
        }
    }
}
