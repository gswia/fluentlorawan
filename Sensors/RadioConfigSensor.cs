using IotHubFunction.Configuration;
using IotHubFunction.Readings;
using System.Text.Json;

namespace IotHubFunction.Sensors
{
    public class RadioConfigSensor : Sensor
    {
        public RadioConfigSensor()
        {
            SensorType = "RadioConfig";
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
            if (message.FPort != 5 || message.Object == null) return new List<Reading>();
            if (!message.Object.ContainsKey("Freq_Band") || !message.Object.ContainsKey("Sub_Band")) return new List<Reading>();

            return new List<Reading>
            {
                new LoRaConfig
                {
                    TimestampUTC = message.Time,
                    AccountId = accountId,
                    ApplicationId = applicationId,
                    SiteId = siteId,
                    DeviceId = deviceId,
                    MessageId = messageId,
                    SensorId = SensorId.ToString(),
                    FrequencyBand = (int)((JsonElement)message.Object["Freq_Band"]).GetDouble(),
                    SubBand = (int)((JsonElement)message.Object["Sub_Band"]).GetDouble()
                }
            };
        }
    }
}
