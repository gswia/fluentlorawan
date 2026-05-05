using IotHubFunction.Devices;
using IotHubFunction.Readings;
using System.Text.Json;

namespace IotHubFunction.Sensors
{
    public class IlluminationSensor : Sensor
    {
        public IlluminationSensor()
        {
            SensorType = "Illumination";
        }

        public override List<Reading> CreateReadings(
            ChirpStackMessage message, 
            string accountId,
            string groupId,
            string deviceId, 
            string messageId,
            Device device)
        {
            if (message.Object == null) return new List<Reading>();
            if (!message.Object.ContainsKey("ILL_lx")) return new List<Reading>();

            // LHT65N doesn't include Systimestamp for illumination readings, use message timestamp
            var deviceTimestamp = message.Time.ToUniversalTime();

            return new List<Reading>
            {
                new Illumination
                {
                    TimestampUTC = deviceTimestamp,
                    AccountId = accountId,
                    GroupId = groupId,
                    DeviceId = deviceId,
                    MessageId = messageId,
                    SensorId = SensorId.ToString(),
                    ValueLux = ((JsonElement)message.Object["ILL_lx"]).GetDouble()
                }
            };
        }
    }
}
