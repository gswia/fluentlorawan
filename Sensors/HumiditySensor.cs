using IotHubFunction.Configuration;
using IotHubFunction.Readings;
using System.Text.Json;

namespace IotHubFunction.Sensors
{
    public class HumiditySensor : Sensor
    {
        public HumiditySensor()
        {
            SensorType = "Humidity";
        }

        public override List<Reading> CreateReadings(
            ChirpStackMessage message, 
            string accountId,
            string applicationId,
            string siteId,
            string deviceId, 
            string messageId,
            Device device)
        {
            if (message.FPort != 2 || message.Object == null) return new List<Reading>();
            if (!message.Object.ContainsKey("Hum_SHT") || !message.Object.ContainsKey("Systimestamp")) return new List<Reading>();

            var systimestamp = (long)((JsonElement)message.Object["Systimestamp"]).GetDouble();
            var deviceTimestamp = DateTimeOffset.FromUnixTimeSeconds(systimestamp).UtcDateTime;

            return new List<Reading>
            {
                new Humidity
                {
                    TimestampUTC = deviceTimestamp,
                    AccountId = accountId,
                    ApplicationId = applicationId,
                    SiteId = siteId,
                    DeviceId = deviceId,
                    MessageId = messageId,
                    SensorId = SensorId.ToString(),
                    ValueRH = ((JsonElement)message.Object["Hum_SHT"]).GetDouble()
                }
            };
        }
    }
}
