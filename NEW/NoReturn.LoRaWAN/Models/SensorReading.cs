namespace NoReturn.LoRaWAN.Models
{
    public abstract class SensorReading : Reading
    {
        public string SensorId { get; set; } = string.Empty;
    }
}
