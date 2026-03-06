using IotHubFunction.Readings;
using System.Text.Json.Serialization;

namespace IotHubFunction.Configuration
{
    [JsonDerivedType(typeof(LHT52Device), typeDiscriminator: "LHT52")]
    [JsonDerivedType(typeof(LHT65NDevice), typeDiscriminator: "LHT65N")]
    [JsonDerivedType(typeof(LDS02Device), typeDiscriminator: "LDS02")]
    public abstract class Device
    {
        public string DeviceId { get; set; } = string.Empty;
        public List<Sensor> Sensors { get; set; } = new();
        
        public virtual List<Reading> CreateReadings(ChirpStackMessage message, string accountId, string applicationId, string siteId)
        {
            var readings = new List<Reading>();
            var deviceId = message.DeviceInfo.DevEui;
            var messageId = message.DeduplicationId;

            foreach (var sensor in Sensors)
            {
                var sensorReadings = sensor.CreateReadings(message, accountId, applicationId, siteId, deviceId, messageId, this);
                readings.AddRange(sensorReadings);
            }

            return readings;
        }
    }
}
