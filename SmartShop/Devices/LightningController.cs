using System;
using System.Collections.Generic;
using System.Text;
using SmartShop.Core;
using SmartShop.Models;

namespace SmartShop.Devices
{
    public class LightningController : Controller
    {
        public bool IsOn {  get; set; }
        public int BrightnessPercent { get; set; }

        public override TelemetryPayload GetTelemetry()
        {
            throw new NotImplementedException();
        }

        public override CommandResult SetConfig(CommandRequest cmd)
        {
            throw new NotImplementedException();
        }
    }
}
