using NoReturn.LoRaWAN.Readings;
using System.Text.Json.Serialization;

namespace NoReturn.LoRaWAN.Sensors
{
    [JsonDerivedType(typeof(TemperatureSensor), typeDiscriminator: "Temperature")]
    [JsonDerivedType(typeof(HumiditySensor), typeDiscriminator: "Humidity")]
    [JsonDerivedType(typeof(VoltageSensor), typeDiscriminator: "Voltage")]
    [JsonDerivedType(typeof(IlluminationSensor), typeDiscriminator: "Illumination")]
    [JsonDerivedType(typeof(DeviceModelSensor), typeDiscriminator: "DeviceModel")]
    [JsonDerivedType(typeof(FirmwareSensor), typeDiscriminator: "Firmware")]
    [JsonDerivedType(typeof(RadioConfigSensor), typeDiscriminator: "RadioConfig")]
    [JsonDerivedType(typeof(GatewaySensor), typeDiscriminator: "Gateway")]
    [JsonDerivedType(typeof(TemperatureProbeSensor), typeDiscriminator: "TemperatureProbe")]
    [JsonDerivedType(typeof(DoorSensor), typeDiscriminator: "Door")]
    [JsonDerivedType(typeof(VibrationSensor), typeDiscriminator: "Vibration")]
    [JsonDerivedType(typeof(AccelerationSensor), typeDiscriminator: "Acceleration")]
    public abstract class Sensor
    {
        public Guid SensorId { get; set; }
        public string SensorType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public abstract List<Reading> CreateReadings(
            ChirpStackMessage message, 
            string accountId,
            string groupId,
            string deviceId, 
            string messageId,
            NoReturn.LoRaWAN.Devices.Device device);
    }
}
