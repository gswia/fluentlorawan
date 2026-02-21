using IotHubFunction.Configuration;
using IotHubFunction.Readings;

namespace IotHubFunction.Sensors
{
    public class GatewaySensor : Sensor
    {
        public GatewaySensor()
        {
            SensorType = "Gateway";
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
            if (message.RxInfo == null || !message.RxInfo.Any()) return new List<Reading>();

            var readings = new List<Reading>();

            // Create a reading for each gateway that received the message
            foreach (var gatewayRx in message.RxInfo)
            {
                readings.Add(new Gateway
                {
                    TimestampUTC = message.Time,
                    AccountId = accountId,
                    ApplicationId = applicationId,
                    SiteId = siteId,
                    DeviceId = deviceId,
                    MessageId = messageId,
                    SensorId = gatewayRx.GatewayId,
                    GatewayId = gatewayRx.GatewayId,
                    Rssi = gatewayRx.Rssi,
                    Snr = gatewayRx.Snr,
                    GwTime = gatewayRx.GwTime,
                    NsTime = gatewayRx.NsTime,
                    Latitude = gatewayRx.Location.Latitude,
                    Longitude = gatewayRx.Location.Longitude
                });
            }

            return readings;
        }
    }
}
