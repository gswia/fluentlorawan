using IotHubFunction.Configuration;
using IotHubFunction.Readings;
using System.Text.Json;

namespace IotHubFunction.Sensors
{
    public class VoltageSensor : Sensor
    {
        public VoltageSensor()
        {
            SensorType = "Voltage";
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
            // Battery voltage can appear in fPort 2 (LHT65N regular messages) or fPort 5 (configuration messages)
            if ((message.FPort != 2 && message.FPort != 5) || message.Object == null) return new List<Reading>();
            if (!message.Object.ContainsKey("Bat_mV")) return new List<Reading>();

            // Use Systimestamp if available (LHT52), otherwise use message timestamp (LHT65N or fPort 5)
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
                new Voltage
                {
                    TimestampUTC = deviceTimestamp,
                    AccountId = accountId,
                    ApplicationId = applicationId,
                    SiteId = siteId,
                    DeviceId = deviceId,
                    MessageId = messageId,
                    SensorId = SensorId.ToString(),
                    ValueV = ((JsonElement)message.Object["Bat_mV"]).GetDouble() / 1000.0
                }
            };
        }
    }
}
