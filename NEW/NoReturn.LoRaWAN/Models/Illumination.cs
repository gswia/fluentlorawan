namespace NoReturn.LoRaWAN.Models
{
    public class Illumination : SensorReading
    {
        public double ValueLux { get; set; }

        public override object GetPayload() => new { ValueLux };
    }
}
