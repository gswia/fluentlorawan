namespace IotHubFunction.Readings
{
    public class Temperature : SensorReading
    {
        public double ValueC { get; set; }

        public override object GetPayload() => new { ValueC };
    }
}
