namespace IotHubFunction.Readings
{
    public class ProbeId : SensorReading
    {
        public string Value { get; set; }

        public override object GetPayload() => new { Value };
    }
}
