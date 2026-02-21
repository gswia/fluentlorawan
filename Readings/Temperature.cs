namespace IotHubFunction.Readings
{
    public class Temperature : Reading
    {
        public double ValueC { get; set; }

        public override object GetPayload() => new { ValueC };
    }
}
