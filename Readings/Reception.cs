namespace IotHubFunction.Readings
{
    public class Reception : Reading
    {
        public int Rssi { get; set; }
        public double Snr { get; set; }
        public DateTime GwTime { get; set; }
        public DateTime NsTime { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
