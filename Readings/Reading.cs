namespace IotHubFunction.Readings
{
    public abstract class Reading
    {
        public DateTime TimestampUTC { get; set; }
        public string AccountId { get; set; } = string.Empty;
        public string ApplicationId { get; set; } = string.Empty;
        public string SiteId { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        public string SensorId { get; set; } = string.Empty;
        public string MessageId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;

        protected Reading()
        {
            Type = GetType().Name;
        }

        public abstract object GetPayload();
    }
}
