using IotHubFunction.Sensors;
using IotHubFunction.Readings;

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
                SensorId = GatewaySensor.SensorId,
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
                var systimestamp = Convert.ToInt64(message.Object["Systimestamp"]);
                var deviceTimestamp = DateTimeOffset.FromUnixTimeSeconds(systimestamp).UtcDateTime;
                
                // Internal Temperature (TempC_SHT)
                readings.Add(new Temperature
                {
                    TimestampUTC = deviceTimestamp,
                    AccountId = accountId,
                    DeviceId = deviceId,
                    MessageId = messageId,
                    SensorId = TemperatureSensor.SensorId,
                    ValueC = Convert.ToDouble(message.Object["TempC_SHT"])
                });
                
                // Humidity (Hum_SHT)
                readings.Add(new Humidity
                {
                    TimestampUTC = deviceTimestamp,
                    AccountId = accountId,
                    DeviceId = deviceId,
                    MessageId = messageId,
                    SensorId = HumiditySensor.SensorId,
                    ValueRH = Convert.ToDouble(message.Object["Hum_SHT"])
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
                    ValueV = Convert.ToDouble(message.Object["Bat_mV"]) / 1000.0
                });
                
                // Device Model (Sensor_Model)
                readings.Add(new ModelNumber
                {
                    TimestampUTC = networkTimestamp,
                    AccountId = accountId,
                    DeviceId = deviceId,
                    MessageId = messageId,
                    SensorId = DeviceModelSensor.SensorId,
                    Value = Convert.ToInt32(message.Object["Sensor_Model"])
                });
                
                // Firmware Version
                readings.Add(new Readings.Version
                {
                    TimestampUTC = networkTimestamp,
                    AccountId = accountId,
                    DeviceId = deviceId,
                    MessageId = messageId,
                    SensorId = FirmwareSensor.SensorId,
                    Value = message.Object["Firmware_Version"].ToString()
                });
                
                // LoRa Config (Freq_Band + Sub_Band)
                readings.Add(new LoRaConfig
                {
                    TimestampUTC = networkTimestamp,
                    AccountId = accountId,
                    DeviceId = deviceId,
                    MessageId = messageId,
                    SensorId = RadioConfigSensor.SensorId,
                    FrequencyBand = Convert.ToInt32(message.Object["Freq_Band"]),
                    SubBand = Convert.ToInt32(message.Object["Sub_Band"])
                });
            }
            
            return readings;
        }
    }
}
