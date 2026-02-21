using IotHubFunction.Sensors;
using IotHubFunction.Readings;
using System.Text.Json;

namespace IotHubFunction.Configuration
{
    public class LHT52Device : Device
    {
        public TemperatureSensor TemperatureSensor { get; set; }
        public HumiditySensor HumiditySensor { get; set; }
        public VoltageSensor VoltageSensor { get; set; }
        public DeviceModelSensor DeviceModelSensor { get; set; }
        public FirmwareSensor FirmwareSensor { get; set; }
        public RadioConfigSensor RadioConfigSensor { get; set; }
        public GatewaySensor GatewaySensor { get; set; }

        public override List<Reading> CreateReadings(ChirpStackMessage message, string accountId)
        {
            var readings = new List<Reading>();
            
            if (message.Object == null) return readings;
            
            var deviceId = message.DeviceInfo.DevEui;
            var messageId = message.DeduplicationId;
            var fPort = message.FPort;
            
            // Extract gateway-specific data: prefer assigned gateway, fall back to strongest signal
            var gatewayRx = message.RxInfo.FirstOrDefault(rx => rx.GatewayId == GatewayId)
                            ?? message.RxInfo.OrderByDescending(rx => rx.Rssi).First();
            
            var networkTimestamp = message.Time;
            readings.Add(new Gateway
            {
                TimestampUTC = networkTimestamp,
                AccountId = accountId,
                DeviceId = deviceId,
                MessageId = messageId,
                SensorId = gatewayRx.GatewayId, // Use actual gateway ID as sensor ID
                GatewayId = gatewayRx.GatewayId,
                Rssi = gatewayRx.Rssi,
                Snr = gatewayRx.Snr,
                GwTime = gatewayRx.GwTime,
                NsTime = gatewayRx.NsTime,
                Latitude = gatewayRx.Location.Latitude,
                Longitude = gatewayRx.Location.Longitude
            });
            
            // fPort 2: Temperature, Humidity (uses device timestamp)
            if (fPort == 2)
            {
                var systimestamp = (long)((JsonElement)message.Object["Systimestamp"]).GetDouble();
                var deviceTimestamp = DateTimeOffset.FromUnixTimeSeconds(systimestamp).UtcDateTime;
                
                // Internal Temperature (TempC_SHT)
                readings.Add(new Temperature
                {
                    TimestampUTC = deviceTimestamp,
                    AccountId = accountId,
                    DeviceId = deviceId,
                    MessageId = messageId,
                    SensorId = TemperatureSensor.SensorId,
                    ValueC = ((JsonElement)message.Object["TempC_SHT"]).GetDouble()
                });
                
                // Humidity (Hum_SHT)
                readings.Add(new Humidity
                {
                    TimestampUTC = deviceTimestamp,
                    AccountId = accountId,
                    DeviceId = deviceId,
                    MessageId = messageId,
                    SensorId = HumiditySensor.SensorId,
                    ValueRH = ((JsonElement)message.Object["Hum_SHT"]).GetDouble()
                });
            }
            
            // fPort 5: Battery, Model, Firmware, LoRa Config (uses network timestamp)
            if (fPort == 5)
            {
                // Battery Voltage (Bat_mV)
                readings.Add(new Voltage
                {
                    TimestampUTC = networkTimestamp,
                    AccountId = accountId,
                    DeviceId = deviceId,
                    MessageId = messageId,
                    SensorId = VoltageSensor.SensorId,
                    ValueV = ((JsonElement)message.Object["Bat_mV"]).GetDouble() / 1000.0
                });
                
                // Device Model (Sensor_Model)
                readings.Add(new ModelNumber
                {
                    TimestampUTC = networkTimestamp,
                    AccountId = accountId,
                    DeviceId = deviceId,
                    MessageId = messageId,
                    SensorId = DeviceModelSensor.SensorId,
                    Value = (int)((JsonElement)message.Object["Sensor_Model"]).GetDouble()
                });
                
                // Firmware Version
                readings.Add(new Readings.Version
                {
                    TimestampUTC = networkTimestamp,
                    AccountId = accountId,
                    DeviceId = deviceId,
                    MessageId = messageId,
                    SensorId = FirmwareSensor.SensorId,
                    Value = ((JsonElement)message.Object["Firmware_Version"]).GetString()
                });
                
                // LoRa Config (Freq_Band + Sub_Band)
                readings.Add(new LoRaConfig
                {
                    TimestampUTC = networkTimestamp,
                    AccountId = accountId,
                    DeviceId = deviceId,
                    MessageId = messageId,
                    SensorId = RadioConfigSensor.SensorId,
                    FrequencyBand = (int)((JsonElement)message.Object["Freq_Band"]).GetDouble(),
                    SubBand = (int)((JsonElement)message.Object["Sub_Band"]).GetDouble()
                });
            }
            
            return readings;
        }
    }
}
