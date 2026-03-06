using IotHubFunction.Readings;
using System.Text.Json.Serialization;

namespace IotHubFunction.Configuration
{
    [JsonDerivedType(typeof(IotHubFunction.Sensors.TemperatureSensor), typeDiscriminator: "Temperature")]
    [JsonDerivedType(typeof(IotHubFunction.Sensors.HumiditySensor), typeDiscriminator: "Humidity")]
    [JsonDerivedType(typeof(IotHubFunction.Sensors.VoltageSensor), typeDiscriminator: "Voltage")]
    [JsonDerivedType(typeof(IotHubFunction.Sensors.IlluminationSensor), typeDiscriminator: "Illumination")]
    [JsonDerivedType(typeof(IotHubFunction.Sensors.DeviceModelSensor), typeDiscriminator: "DeviceModel")]
    [JsonDerivedType(typeof(IotHubFunction.Sensors.FirmwareSensor), typeDiscriminator: "Firmware")]
    [JsonDerivedType(typeof(IotHubFunction.Sensors.RadioConfigSensor), typeDiscriminator: "RadioConfig")]
    [JsonDerivedType(typeof(IotHubFunction.Sensors.GatewaySensor), typeDiscriminator: "Gateway")]
    [JsonDerivedType(typeof(IotHubFunction.Sensors.TemperatureProbeSensor), typeDiscriminator: "TemperatureProbe")]
    [JsonDerivedType(typeof(IotHubFunction.Sensors.DoorSensor), typeDiscriminator: "Door")]
    [JsonDerivedType(typeof(IotHubFunction.Sensors.VibrationSensor), typeDiscriminator: "Vibration")]
    [JsonDerivedType(typeof(IotHubFunction.Sensors.AccelerationSensor), typeDiscriminator: "Acceleration")]
    public abstract class Sensor
    {
        public Guid SensorId { get; set; }
        public string SensorType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public abstract List<Reading> CreateReadings(
            ChirpStackMessage message, 
            string accountId,
            string applicationId,
            string siteId,
            string deviceId, 
            string messageId,
            Device device);
    }
}
