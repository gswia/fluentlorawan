using IotHubFunction.Configuration;
using IotHubFunction.Readings;
using System.Text.Json;

namespace IotHubFunction.Sensors
{
    public class FirmwareSensor : Sensor
    {
        public FirmwareSensor()
        {
            SensorType = "Firmware";
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
            if (!message.Object.ContainsKey("Firmware_Version")) return new List<Reading>();

            return new List<Reading>
            {
                new Readings.Version
                {
                    TimestampUTC = message.Time,
                    AccountId = accountId,
                    ApplicationId = applicationId,
                    SiteId = siteId,
                    DeviceId = deviceId,
                    MessageId = messageId,
                    SensorId = SensorId.ToString(),
                    Value = ((JsonElement)message.Object["Firmware_Version"]).GetString()
                }
            };
        }
    }
}
