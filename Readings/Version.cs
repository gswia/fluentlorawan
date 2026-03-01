namespace IotHubFunction.Readings
{
    public class Version : SensorReading
    {
        public string Value { get; set; }

        public override object GetPayload() => new { Value };
    }
}
