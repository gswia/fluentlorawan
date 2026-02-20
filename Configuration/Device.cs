using IotHubFunction.Readings;

namespace IotHubFunction.Configuration
{
    public abstract class Device
    {
        public string DeviceId { get; set; }
        public abstract List<Reading> CreateReadings(ChirpStackMessage message, string accountId);
    }
}
