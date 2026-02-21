namespace IotHubFunction.Readings
{
    public class LoRaConfig : Reading
    {
        public int FrequencyBand { get; set; }
        public int SubBand { get; set; }

        public override object GetPayload() => new { FrequencyBand, SubBand };
    }
}
