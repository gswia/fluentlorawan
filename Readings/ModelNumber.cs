namespace IotHubFunction.Readings
{
    public class ModelNumber : SensorReading
    {
        public int Value { get; set; }

        public override object GetPayload() => new { Value };
    }
}
