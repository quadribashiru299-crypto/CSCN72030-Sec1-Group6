using System;
using System.Collections.Generic;
using System.Text;
using SmartShop.Models;
using SmartShop.Core;

namespace SmartShop.Devices
{
    public class VentilationController : Controller
    {
        public string HvacMode => CurrentConfig.State;
        public int FanSpeedPercent
        {
            get
            {
                if (CurrentConfig.Value != null)
                {
                    return CurrentConfig.Value.Value;
                }
                else
                {
                    return 0;
                }
            }
        }

        public VentilationController(int id, string name, string HvacMode, int fanSpeedPercent)
        {
            this.DeviceId = id;
            this.DeviceName = name;
            this.LastUpdated = DateTime.UtcNow;
            this.IsDataAvailable = true;

            if (fanSpeedPercent < 0) { fanSpeedPercent = 0; }
            if (fanSpeedPercent > 100) { fanSpeedPercent = 100; }
            if (HvacMode != "Off"
                && HvacMode != "Auto"
                && HvacMode != "Cool"
                && HvacMode != "Boost"
                && HvacMode != "Heat")
            {
                HvacMode = "Off";
            }

            this.CurrentConfig = new DeviceConfig
            {
                State = HvacMode,
                Value = fanSpeedPercent
            };
        }

        private void TurnOnAuto()
        {
            DeviceConfig newConfig = new DeviceConfig
            {
                State = "Auto",
                Value = this.CurrentConfig.Value
            };
            CurrentConfig = newConfig;
        }

        private void TurnOnCool()
        {
            DeviceConfig newConfig = new DeviceConfig
            {
                State = "Cool",
                Value = this.CurrentConfig.Value
            };
            CurrentConfig = newConfig;
        }

        private void TurnOnBoost()
        {
            DeviceConfig newConfig = new DeviceConfig
            {
                State = "Boost",
                Value = this.CurrentConfig.Value
            };
            CurrentConfig = newConfig;
        }

        private void TurnOnHeat()
        {
            DeviceConfig newConfig = new DeviceConfig
            {
                State = "Heat",
                Value = this.CurrentConfig.Value
            };
            CurrentConfig = newConfig;
        }

        private void TurnOff()
        {
            DeviceConfig newConfig = new DeviceConfig
            {
                State = "Off",
                Value = this.CurrentConfig.Value
            };
            CurrentConfig = newConfig;
        }

        private void SetFanSpeed(int fanSpeed)
        {
            if (fanSpeed < 0) { fanSpeed = 0; }
            if (fanSpeed > 100) { fanSpeed = 100; }
            DeviceConfig newConfig = new DeviceConfig
            {
                State = this.CurrentConfig.State,
                Value = fanSpeed
            };
            CurrentConfig = newConfig;
        }

        public override TelemetryPayload GetTelemetry()
        {
            int fanSpeed = FanSpeedPercent;
            if (CurrentConfig.State == "Off")
            {
                fanSpeed = 0;
            }
            TelemetryPayload payload = new TelemetryPayload(DeviceId, fanSpeed, "%", Status);
            return payload;
        }

        public override CommandResult SetConfig(CommandRequest cmd)
        {
            if (cmd == null || cmd.RequestedState == null)
            {
                string errorMessage = "No command was received.";

                CommandResult errResult = new CommandResult(false, CurrentConfig.State, errorMessage);
                return errResult;
            }
            if (cmd.DeviceId != this.DeviceId)
            {
                string errorMessage = $"Command was addressed to device {cmd.DeviceId}, not the HVAC controller ({this.DeviceId}).";

                CommandResult errResult = new CommandResult(false, CurrentConfig.State, errorMessage);
                return errResult;
            }
            if (string.IsNullOrWhiteSpace(cmd.AdminUserId))
            {
                string errorMessage = "Command rejected: an authenticated administrator is required.";
                CommandResult errResult = new CommandResult(false, CurrentConfig.State, errorMessage);
                return errResult;
            }

            if (cmd.RequestedState.Equals("Off", StringComparison.OrdinalIgnoreCase))
            {
                this.TurnOff();
                LastUpdated = DateTime.UtcNow;
                CommandResult offResult = new CommandResult(true, CurrentConfig.State, null);
                return offResult;
            }

            if (cmd.RequestedState.Equals("Auto", StringComparison.OrdinalIgnoreCase))
            {
                this.TurnOnAuto();
                LastUpdated = DateTime.UtcNow;
                CommandResult autoResult = new CommandResult(true, CurrentConfig.State, null);
                return autoResult;
            }

            if (cmd.RequestedState.Equals("Cool", StringComparison.OrdinalIgnoreCase))
            {
                this.TurnOnCool();
                LastUpdated = DateTime.UtcNow;
                CommandResult coolResult = new CommandResult(true, CurrentConfig.State, null);
                return coolResult;
            }

            if (cmd.RequestedState.Equals("Boost", StringComparison.OrdinalIgnoreCase))
            {
                this.TurnOnBoost();
                LastUpdated = DateTime.UtcNow;
                CommandResult boostResult = new CommandResult(true, CurrentConfig.State, null);
                return boostResult;
            }

            if (cmd.RequestedState.Equals("Heat", StringComparison.OrdinalIgnoreCase))
            {
                this.TurnOnHeat();
                LastUpdated = DateTime.UtcNow;
                CommandResult heatResult = new CommandResult(true, CurrentConfig.State, null);
                return heatResult;
            }

            // tryng to convert string to integer
            if (int.TryParse(cmd.RequestedState.Trim(), out int fanSpeed) && fanSpeed >= 0 && fanSpeed <= 100)
            {
                SetFanSpeed(fanSpeed);
                if(CurrentConfig.State == "Off" && fanSpeed > 0)
                {
                    this.TurnOnAuto();
                } 

                LastUpdated = DateTime.UtcNow;
                CommandResult result = new CommandResult(true, CurrentConfig.State, null);
                return result;
            }

            // if it isn't a valid HVAC mode (auto, cool, boost, heat, off) or an int 0–100:
            string invalidMsg = $"Invalid HVAC command '{cmd.RequestedState}'. Expected 'Auto', 'Cool', 'Boost', 'Heat', 'Off', or fan speed 0-100.";
            CommandResult invalidMsgResult = new CommandResult(false, CurrentConfig.State, invalidMsg);
            return invalidMsgResult;
        }
    }
}
