namespace NoReturn.LoRaWAN.Graph
{
    public class GraphSensor
    {
        public string Id { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        public SensorType SensorType { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}
