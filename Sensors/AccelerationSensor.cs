using IotHubFunction.Configuration;
using IotHubFunction.Readings;
using System.Text.Json;

namespace IotHubFunction.Sensors
{
    public class AccelerationSensor : Sensor
    {
        public AccelerationSensor()
        {
            SensorType = "Acceleration";
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
            
            // At least one acceleration axis must be present
            if (!message.Object.ContainsKey("Max_Acc_X") && 
                !message.Object.ContainsKey("Max_Acc_Y") &&
                !message.Object.ContainsKey("Max_Acc_Z"))
                return new List<Reading>();

            var acceleration = new Acceleration
            {
                TimestampUTC = message.Time.ToUniversalTime(),
                AccountId = accountId,
                ApplicationId = applicationId,
                SiteId = siteId,
                DeviceId = deviceId,
                MessageId = messageId,
                SensorId = SensorId.ToString()
            };

            // All axes are optional
            if (message.Object.ContainsKey("Max_Acc_X"))
                acceleration.MaxAccX = ((JsonElement)message.Object["Max_Acc_X"]).GetDouble();
            
            if (message.Object.ContainsKey("Max_Acc_Y"))
                acceleration.MaxAccY = ((JsonElement)message.Object["Max_Acc_Y"]).GetDouble();
            
            if (message.Object.ContainsKey("Max_Acc_Z"))
                acceleration.MaxAccZ = ((JsonElement)message.Object["Max_Acc_Z"]).GetDouble();

            return new List<Reading> { acceleration };
        }
    }
}
