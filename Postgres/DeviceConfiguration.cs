namespace NoReturn.LoRaWAN.Postgres;

public class DeviceConfiguration
{
    public Guid AccountId { get; set; }
    public Guid GroupId { get; set; }
    public Dictionary<string, Guid> SensorMappings { get; set; } = new();

    public bool TryGetSensorId(string sensorType, out Guid sensorId)
    {
        return SensorMappings.TryGetValue(sensorType, out sensorId);
    }
}
