using NoReturn.LoRaWAN.Devices;
using NoReturn.LoRaWAN.Readings;

namespace NoReturn.LoRaWAN.Sensors
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
            string groupId,
            string deviceId, 
            string messageId,
            Device device)
        {
            // TODO: Implement when probe data format is known
            return new List<Reading>();
        }
    }
}
