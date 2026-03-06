using IotHubFunction.Configuration;
using IotHubFunction.Readings;
using System.Text.Json;

namespace IotHubFunction.Sensors
{
    public class VibrationSensor : Sensor
    {
        public VibrationSensor()
        {
            SensorType = "Vibration";
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
            if (message.Object == null) return new List<Reading>();
            
            // At least one vibration-related key must be present
            if (!message.Object.ContainsKey("Vib_Count") && 
                !message.Object.ContainsKey("Work_Min") &&
                !message.Object.ContainsKey("Alarm") &&
                !message.Object.ContainsKey("TDC"))
                return new List<Reading>();

            var vibration = new Vibration
            {
                TimestampUTC = message.Time.ToUniversalTime(),
                AccountId = accountId,
                ApplicationId = applicationId,
                SiteId = siteId,
                DeviceId = deviceId,
                MessageId = messageId,
                SensorId = SensorId.ToString()
            };

            // All fields are optional depending on MOD byte
            if (message.Object.ContainsKey("Vib_Count"))
                vibration.VibCount = (int)((JsonElement)message.Object["Vib_Count"]).GetDouble();
            
            if (message.Object.ContainsKey("Work_Min"))
                vibration.WorkMin = (int)((JsonElement)message.Object["Work_Min"]).GetDouble();
            
            if (message.Object.ContainsKey("Alarm"))
                vibration.Alarm = (int)((JsonElement)message.Object["Alarm"]).GetDouble();
            
            if (message.Object.ContainsKey("TDC"))
                vibration.TDC = (int)((JsonElement)message.Object["TDC"]).GetDouble();

            return new List<Reading> { vibration };
        }
    }
}
