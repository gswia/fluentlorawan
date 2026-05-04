namespace IotHubFunction.Configuration
{
    public static class SensorFactory
    {
        private static readonly Dictionary<string, Func<Sensor>> _sensorTypes = new()
        {
            { "Temperature", () => new IotHubFunction.Sensors.TemperatureSensor() },
            { "Humidity", () => new IotHubFunction.Sensors.HumiditySensor() },
            { "Voltage", () => new IotHubFunction.Sensors.VoltageSensor() },
            { "Illumination", () => new IotHubFunction.Sensors.IlluminationSensor() },
            { "DeviceModel", () => new IotHubFunction.Sensors.DeviceModelSensor() },
            { "Firmware", () => new IotHubFunction.Sensors.FirmwareSensor() },
            { "RadioConfig", () => new IotHubFunction.Sensors.RadioConfigSensor() },
            { "Gateway", () => new IotHubFunction.Sensors.GatewaySensor() },
            { "TemperatureProbe", () => new IotHubFunction.Sensors.TemperatureProbeSensor() },
            { "Door", () => new IotHubFunction.Sensors.DoorSensor() },
            { "Vibration", () => new IotHubFunction.Sensors.VibrationSensor() },
            { "Acceleration", () => new IotHubFunction.Sensors.AccelerationSensor() }
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
