namespace IotHubFunction.DataAccess.Models
{
    public class Humidity : SensorReading
    {
        public double ValueRH { get; set; }

        public override object GetPayload() => new { ValueRH };
    }
}
