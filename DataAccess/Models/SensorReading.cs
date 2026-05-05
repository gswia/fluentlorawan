namespace IotHubFunction.DataAccess.Models
{
    public abstract class SensorReading : Reading
    {
        public string SensorId { get; set; } = string.Empty;
    }
}
