namespace IotHubFunction.Readings
{
    public class ModelNumber : Reading
    {
        public int Value { get; set; }

        public override object GetPayload() => new { Value };
    }
}
