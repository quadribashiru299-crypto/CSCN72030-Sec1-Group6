using System;
using System.Collections.Generic;
using System.Text;
using SmartShop.Core;
using SmartShop.Models;

namespace SmartShop.Devices
{
    public class LightingController : Controller
    {
        public bool IsOn => CurrentConfig.State == "On";
        public int BrightnessPercent
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
        //public int BrightnessPercent => CurrentConfig.Value ?? 0; // this is the same, just shorter syntax

        public LightingController(int id, string name, bool isOn, int brightnessPercent)
        {
            this.DeviceId = id;
            this.DeviceName = name;
            this.LastUpdated = DateTime.UtcNow;
            this.IsDataAvailable = true;

            if (brightnessPercent < 0) { brightnessPercent = 0; }
            if (brightnessPercent > 100) { brightnessPercent = 100; }
            this.CurrentConfig = new DeviceConfig
            {
                State = isOn ? "On" : "Off", //if isOn is true, State = "On", else State = "Off"
                Value = brightnessPercent
            };
        }

        private void TurnOn()
        {
            DeviceConfig newConfig = new DeviceConfig
            {
                State = "On",
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

        private void SetBrightness(int brightness)
        {
            if(brightness < 0) { brightness = 0; }
            if (brightness > 100) { brightness = 100; }
            DeviceConfig newConfig = new DeviceConfig
            {
                State = this.CurrentConfig.State,
                Value = brightness
            };
            CurrentConfig = newConfig;
        }

        public override TelemetryPayload GetTelemetry()
        {
            int brightness = BrightnessPercent;
            if(CurrentConfig.State == "Off")
            {
                brightness = 0;
            }
            TelemetryPayload payload = new TelemetryPayload(DeviceId, brightness, "%", Status);
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
            if (cmd.DeviceId != this.DeviceId) {
                string errorMessage = $"Command was addressed to device {cmd.DeviceId}, not the lighting controller ({this.DeviceId}).";

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

            if (cmd.RequestedState.Equals("On", StringComparison.OrdinalIgnoreCase))
            {
                this.TurnOn();
                LastUpdated = DateTime.UtcNow;
                CommandResult onResult = new CommandResult(true, CurrentConfig.State, null);
                return onResult;
            }

            // tryng to convert string to integer
            if (int.TryParse(cmd.RequestedState.Trim(), out int brightness) && brightness >= 0 && brightness <= 100)
            {
                SetBrightness(brightness);

                LastUpdated = DateTime.UtcNow;
                CommandResult result = new CommandResult(true, CurrentConfig.State, null);
                return result;
            }

            // if it isn't "On", "Off", or an int 0–100:
            string invalidMsg = $"Invalid lighting command '{cmd.RequestedState}'. Expected 'On', 'Off', or brightness 0-100.";
            CommandResult invalidMsgResult = new CommandResult(false, CurrentConfig.State, invalidMsg);
            return invalidMsgResult;
        }
    }
}
