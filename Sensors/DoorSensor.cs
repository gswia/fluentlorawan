using NoReturn.LoRaWAN.Devices;
using NoReturn.LoRaWAN.Readings;
using System.Text.Json;

namespace NoReturn.LoRaWAN.Sensors
{
    public class DoorSensor : Sensor
    {
        public DoorSensor()
        {
            SensorType = "Door";
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
            if (!message.Object.ContainsKey("Door_Open")) return new List<Reading>();

            var door = new Door
            {
                TimestampUTC = message.Time.ToUniversalTime(),
                AccountId = accountId,
                GroupId = groupId,
                DeviceId = deviceId,
                MessageId = messageId,
                SensorId = SensorId.ToString(),
                DoorOpen = (int)((JsonElement)message.Object["Door_Open"]).GetDouble()
            };

            // Optional fields
            if (message.Object.ContainsKey("Open_Times"))
                door.OpenTimes = (int)((JsonElement)message.Object["Open_Times"]).GetDouble();
            
            if (message.Object.ContainsKey("Open_Duration"))
                door.OpenDuration = (int)((JsonElement)message.Object["Open_Duration"]).GetDouble();
            
            if (message.Object.ContainsKey("Alarm"))
                door.Alarm = (int)((JsonElement)message.Object["Alarm"]).GetDouble();

            return new List<Reading> { door };
        }
    }
}
