namespace IotHubFunction.Readings
{
    public class ProbeType : Reading
    {
        public int Value { get; set; }

        public override object GetPayload() => new { Value };
    }
}
