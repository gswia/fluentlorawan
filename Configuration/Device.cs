using IotHubFunction.Readings;
using System.Text.Json.Serialization;

namespace IotHubFunction.Configuration
{
    [JsonDerivedType(typeof(LHT52Device), typeDiscriminator: "LHT52")]
    public abstract class Device
    {
        public string DeviceId { get; set; } = string.Empty;
        public List<Sensor> Sensors { get; set; } = new();
        public abstract List<Reading> CreateReadings(ChirpStackMessage message, string accountId, string applicationId, string siteId);
    }
}
