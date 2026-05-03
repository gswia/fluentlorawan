namespace NoReturn.LoRaWAN.Models;

/// <summary>
/// Device configuration returned from v1.get_device_config function
/// </summary>
public class DeviceConfiguration
{
    public Guid AccountId { get; set; }
    public Guid GroupId { get; set; }
    public Dictionary<string, Guid> SensorMappings { get; set; } = new();
    
    /// <summary>
    /// Try to get sensor ID for a given sensor type
    /// </summary>
    public bool TryGetSensorId(string sensorType, out Guid sensorId)
    {
        return SensorMappings.TryGetValue(sensorType, out sensorId);
    }
}
