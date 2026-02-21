namespace IotHubFunction.Readings
{
    public class ProbeId : Reading
    {
        public string Value { get; set; }

        public override object GetPayload() => new { Value };
    }
}
