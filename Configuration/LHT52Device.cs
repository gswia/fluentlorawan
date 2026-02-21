using IotHubFunction.Readings;

namespace IotHubFunction.Configuration
{
    public class LHT52Device : Device
    {
        public override List<Reading> CreateReadings(ChirpStackMessage message, string accountId, string applicationId, string siteId)
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
