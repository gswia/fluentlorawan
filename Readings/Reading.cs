namespace IotHubFunction.Readings
{
    public abstract class Reading
    {
        public DateTime TimestampUTC { get; set; }
        public string AccountId { get; set; }
        public string DeviceId { get; set; }
        public string SensorId { get; set; }
        public string MessageId { get; set; }
    }
}
