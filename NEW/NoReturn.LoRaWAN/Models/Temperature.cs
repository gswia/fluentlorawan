namespace NoReturn.LoRaWAN.Models
{
    public class Temperature : SensorReading
    {
        public double ValueC { get; set; }

        public override object GetPayload() => new { ValueC };
    }
}
