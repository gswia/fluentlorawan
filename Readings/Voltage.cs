namespace IotHubFunction.Readings
{
    public class Voltage : Reading
    {
        public double ValueV { get; set; }

        public override object GetPayload() => new { ValueV };
    }
}
