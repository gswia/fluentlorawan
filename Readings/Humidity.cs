namespace IotHubFunction.Readings
{
    public class Humidity : Reading
    {
        public double ValueRH { get; set; }

        public override object GetPayload() => new { ValueRH };
    }
}
