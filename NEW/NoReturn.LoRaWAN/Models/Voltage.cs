namespace NoReturn.LoRaWAN.Models
{
    public class Voltage : SensorReading
    {
        public double ValueV { get; set; }

        public override object GetPayload() => new { ValueV };
    }
}
