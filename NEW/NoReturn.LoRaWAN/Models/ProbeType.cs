namespace NoReturn.LoRaWAN.Models
{
    public class ProbeType : SensorReading
    {
        public int Value { get; set; }

        public override object GetPayload() => new { Value };
    }
}
