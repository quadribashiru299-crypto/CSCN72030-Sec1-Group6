using System;
using SmartShop.Core;
using SmartShop.Models;

namespace SmartShop.Devices
{
    /// <summary>
    /// Store ambient temperature sensor (T06). Reads the looped simulation file
    /// through <see cref="Sensor"/> and classifies the value against the store
    /// comfort bands: too cold below 19 C, too warm from 24 C.
    /// </summary>
    public sealed class StoreTemperatureSensor : Sensor
    {
        public const string SensorKey = "StoreTemperatureSensor";
        public const int DefaultDeviceId = 1;
        public const string DefaultUnit = "C";
        public const string DefaultDeviceName = "Store Temperature";

        public const double DefaultLowCritical = 16.0;
        public const double DefaultLowWarning = 19.0;
        public const double DefaultHighWarning = 24.0;
        public const double DefaultHighCritical = 27.0;

        /// <summary>Values outside this window are corrupted records, not readings.</summary>
        public const double MinPlausibleTemperature = -5.0;
        public const double MaxPlausibleTemperature = 45.0;

        /// <summary>
        /// Reserved for the humidity reading; the current simulation file only
        /// carries temperature, so it stays null.
        /// </summary>
        public double? Humidity { get; set; }

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

        /// <param name="dataFilePath">Looped simulation file, e.g. Data/store_temperature_sensor.txt.</param>
        /// <param name="deviceId">Device identifier, 1 by default.</param>
        /// <param name="thresholds">Optional bands; the store comfort defaults are used when omitted.</param>
        /// <param name="deviceName">Optional display name for the HMI.</param>
        public StoreTemperatureSensor(
            string dataFilePath,
            int deviceId = DefaultDeviceId,
            ThresholdConfig? thresholds = null,
            string? deviceName = null)
        {
            if (dataFilePath is null)
            {
                throw new ArgumentNullException(nameof(dataFilePath));
            }

            SimulationFilePath = dataFilePath;
            DeviceId = deviceId;
            DeviceName = deviceName ?? DefaultDeviceName;
            Unit = DefaultUnit;
            Thresholds = thresholds ?? DefaultThresholds();

            IsDataAvailable = false;
            Status = DeviceStatus.Warning;
        }

        public override bool Validate()
        {
            bool isValid = CandidateValue is double temperature
                && double.IsFinite(temperature)
                && temperature > MinPlausibleTemperature
                && temperature < MaxPlausibleTemperature;

            IsDataAvailable = isValid;
            return isValid;
        }
    }
}
