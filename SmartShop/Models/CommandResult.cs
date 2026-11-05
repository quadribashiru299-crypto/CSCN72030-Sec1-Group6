using System;

namespace SmartShop.Models
{
    /// <summary>
    /// Result returned by an actuator after executing a command (REQ-INT-CTL-001)
    /// </summary>
    public class CommandResult
    {
        public bool IsSuccess { get; init; }
        public string ConfirmedState { get; init; }
        public string ErrorMessage { get; init; }

        public CommandResult(bool isSuccess, string confirmedState, string errorMessage = null)
        {
            IsSuccess = isSuccess;
            ConfirmedState = confirmedState;
            ErrorMessage = errorMessage;
        }
    }
}
