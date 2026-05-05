namespace IotHubFunction.DataAccess.Models
{
    public class LoRaConfig : SensorReading
    {
        public int FrequencyBand { get; set; }
        public int SubBand { get; set; }

        public override object GetPayload() => new { FrequencyBand, SubBand };
    }
}
