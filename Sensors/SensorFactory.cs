using System;
using System.Collections.Generic;

namespace NoReturn.LoRaWAN.Sensors
{
    public static class SensorFactory
    {
        private static readonly Dictionary<string, Func<Sensor>> _sensorTypes = new()
        {
            { "Temperature", () => new TemperatureSensor() },
            { "Humidity", () => new HumiditySensor() },
            { "Voltage", () => new VoltageSensor() },
            { "Illumination", () => new IlluminationSensor() },
            { "DeviceModel", () => new DeviceModelSensor() },
            { "Firmware", () => new FirmwareSensor() },
            { "RadioConfig", () => new RadioConfigSensor() },
            { "Gateway", () => new GatewaySensor() },
            { "TemperatureProbe", () => new TemperatureProbeSensor() },
            { "Door", () => new DoorSensor() },
            { "Vibration", () => new VibrationSensor() },
            { "Acceleration", () => new AccelerationSensor() }
        };

        public static Sensor Create(string sensorType, Guid sensorId)
        {
            if (_sensorTypes.TryGetValue(sensorType, out var factory))
            {
                var sensor = factory();
                sensor.SensorId = sensorId;
                sensor.SensorType = sensorType;
                return sensor;
            }

            throw new NotSupportedException($"Sensor type '{sensorType}' is not supported");
        }
    }
}
