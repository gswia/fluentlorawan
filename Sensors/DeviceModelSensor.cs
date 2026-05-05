using NoReturn.LoRaWAN.Devices;
using NoReturn.LoRaWAN.Readings;
using System.Text.Json;

namespace NoReturn.LoRaWAN.Sensors
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
            string groupId,
            string deviceId, 
            string messageId,
            Device device)
        {
            if (message.Object == null) return new List<Reading>();
            if (!message.Object.ContainsKey("Sensor_Model")) return new List<Reading>();

            return new List<Reading>
            {
                new ModelNumber
                {
                    TimestampUTC = message.Time,
                    AccountId = accountId,
                    GroupId = groupId,
                    DeviceId = deviceId,
                    MessageId = messageId,
                    SensorId = SensorId.ToString(),
                    Value = (int)((JsonElement)message.Object["Sensor_Model"]).GetDouble()
                }
            };
        }
    }
}
