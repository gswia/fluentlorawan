using IotHubFunction.Configuration;
using IotHubFunction.Readings;
using System.Text.Json;

namespace IotHubFunction.Sensors
{
    public class TemperatureSensor : Sensor
    {
        public TemperatureSensor()
        {
            SensorType = "Temperature";
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
            if (!message.Object.ContainsKey("TempC_SHT") || !message.Object.ContainsKey("Systimestamp")) return new List<Reading>();

            var systimestamp = (long)((JsonElement)message.Object["Systimestamp"]).GetDouble();
            var deviceTimestamp = DateTimeOffset.FromUnixTimeSeconds(systimestamp).UtcDateTime;

            return new List<Reading>
            {
                new Temperature
                {
                    TimestampUTC = deviceTimestamp,
                    AccountId = accountId,
                    ApplicationId = applicationId,
                    SiteId = siteId,
                    DeviceId = deviceId,
                    MessageId = messageId,
                    SensorId = SensorId.ToString(),
                    ValueC = ((JsonElement)message.Object["TempC_SHT"]).GetDouble()
                }
            };
        }
    }
}
