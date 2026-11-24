using System;
using SmartShop.Core;
using SmartShop.Models;

namespace SmartShop.Devices
{
    /// <summary>
    /// Cold-chain temperature sensor (REQ-TEL-005). Reads its looped simulation
    /// file through <see cref="Sensor"/> and classifies the temperature against
    /// the cold-chain bands: too cold below 3 C, too warm from 4 C.
    /// </summary>
    public sealed class RefrigeratorTemperature : Sensor
    {
        public const string SensorKey = "RefrigeratorTemperature";
        public const int DefaultDeviceId = 2;
        public const string DefaultUnit = "C";

        public const double DefaultLowCritical = 2.0;
        public const double DefaultLowWarning = 3.0;
        public const double DefaultHighWarning = 4.0;
        public const double DefaultHighCritical = 5.0;

        /// <summary>Wider than any real fridge excursion, narrow enough to reject junk.</summary>
        public const double MinPlausibleTemperature = -10.0;
        public const double MaxPlausibleTemperature = 20.0;

        public static ThresholdConfig DefaultThresholds()
        {
            return new ThresholdConfig
            {
                LowCritical = DefaultLowCritical,
                LowWarning = DefaultLowWarning,
                HighWarning = DefaultHighWarning,
                HighCritical = DefaultHighCritical
            };
        }

        public RefrigeratorTemperature(
            string dataFilePath,
            int deviceId = DefaultDeviceId,
            ThresholdConfig? thresholds = null)
        {
            if (dataFilePath is null)
            {
                throw new ArgumentNullException(nameof(dataFilePath));
            }

            SimulationFilePath = dataFilePath;
            DeviceId = deviceId;
            DeviceName = "Refrigerator Temperature";
            Unit = DefaultUnit;
            Thresholds = thresholds ?? DefaultThresholds();

            // No reading yet: the dashboard must not show a fresh sensor as healthy.
            IsDataAvailable = false;
            Status = DeviceStatus.Warning;
        }

        public override bool Validate()
        {
            bool isValid = CandidateValue is double temperature
                && double.IsFinite(temperature)
                && temperature >= MinPlausibleTemperature
                && temperature <= MaxPlausibleTemperature;

            IsDataAvailable = isValid;
            return isValid;
        }
    }
}
