using IotHubFunction.Configuration;
using IotHubFunction.Readings;

namespace IotHubFunction.Sensors
{
    public class TemperatureProbeSensor : Sensor
    {
        public TemperatureProbeSensor()
        {
            SensorType = "TemperatureProbe";
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
            // TODO: Implement when probe data format is known
            return new List<Reading>();
        }
    }
}
