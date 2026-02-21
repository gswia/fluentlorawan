using IotHubFunction.Configuration;
using IotHubFunction.Readings;
using System.Text.Json;

namespace IotHubFunction.Sensors
{
    public class DeviceModelSensor : Sensor
    {
        public DeviceModelSensor()
        {
            SensorType = "DeviceModel";
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
            if (!message.Object.ContainsKey("Sensor_Model")) return new List<Reading>();

            return new List<Reading>
            {
                new ModelNumber
                {
                    TimestampUTC = message.Time,
                    AccountId = accountId,
                    ApplicationId = applicationId,
                    SiteId = siteId,
                    DeviceId = deviceId,
                    MessageId = messageId,
                    SensorId = SensorId.ToString(),
                    Value = (int)((JsonElement)message.Object["Sensor_Model"]).GetDouble()
                }
            };
        }
    }
}
