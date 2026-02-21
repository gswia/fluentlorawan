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
            if (message.FPort != 5 || message.Object == null) return new List<Reading>();
            if (!message.Object.ContainsKey("Bat_mV")) return new List<Reading>();

            return new List<Reading>
            {
                new Voltage
                {
                    TimestampUTC = message.Time,
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
