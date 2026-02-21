namespace IotHubFunction.Readings
{
    public class Gateway : Reading
    {
        public string GatewayId { get; set; }
        public int Rssi { get; set; }
        public double Snr { get; set; }
        public DateTime GwTime { get; set; }
        public DateTime NsTime { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
