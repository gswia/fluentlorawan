namespace IotHubFunction.Readings
{
    public class Version : Reading
    {
        public string Value { get; set; }

        public override object GetPayload() => new { Value };
    }
}
