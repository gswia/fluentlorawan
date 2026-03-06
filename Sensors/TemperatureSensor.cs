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
            if (message.Object == null) return new List<Reading>();
            if (!message.Object.ContainsKey("TempC_SHT")) return new List<Reading>();

            // Use Systimestamp if available (LHT52), otherwise use message timestamp (LHT65N)
            DateTime deviceTimestamp;
            if (message.Object.ContainsKey("Systimestamp"))
            {
                var systimestamp = (long)((JsonElement)message.Object["Systimestamp"]).GetDouble();
                deviceTimestamp = DateTimeOffset.FromUnixTimeSeconds(systimestamp).UtcDateTime;
            }
            else
            {
                deviceTimestamp = message.Time.ToUniversalTime();
            }

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
