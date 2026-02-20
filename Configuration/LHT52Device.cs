using IotHubFunction.Sensors;
using IotHubFunction.Readings;

namespace IotHubFunction.Configuration
{
    public class LHT52Device : Device
    {
        public TemperatureSensor TemperatureSensor { get; set; }
        public HumiditySensor HumiditySensor { get; set; }
        public TemperatureProbeSensor TemperatureProbeSensor { get; set; }
        public VoltageSensor VoltageSensor { get; set; }
        public DeviceModelSensor DeviceModelSensor { get; set; }
        public FirmwareSensor FirmwareSensor { get; set; }
        public RadioConfigSensor RadioConfigSensor { get; set; }

        public override List<Reading> CreateReadings(ChirpStackMessage message, string accountId)
        {
            var readings = new List<Reading>();
            
            if (message?.Object == null) return readings;
            
            var baseTimestamp = message.Time;
            var deviceId = message.DeviceInfo?.DevEui;
            var messageId = message.DeduplicationId;
            var fPort = message.FPort;
            
            // fPort 2: Temperature, Humidity, External Temp, Probe Type
            if (fPort == 2)
            {
                // Internal Temperature (TempC_SHT)
                if (message.Object.TryGetValue("TempC_SHT", out var tempSht))
                {
                    readings.Add(new Temperature
                    {
                        TimestampUTC = baseTimestamp,
                        AccountId = accountId,
                        DeviceId = deviceId,
                        MessageId = messageId,
                        SensorId = TemperatureSensor?.SensorId,
                        ValueC = Convert.ToDouble(tempSht)
                    });
                }
                
                // Humidity (Hum_SHT)
                if (message.Object.TryGetValue("Hum_SHT", out var humSht))
                {
                    readings.Add(new Humidity
                    {
                        TimestampUTC = baseTimestamp,
                        AccountId = accountId,
                        DeviceId = deviceId,
                        MessageId = messageId,
                        SensorId = HumiditySensor?.SensorId,
                        ValueRH = Convert.ToDouble(humSht)
                    });
                }
                
                // External Temperature (TempC_DS)
                if (message.Object.TryGetValue("TempC_DS", out var tempDs))
                {
                    readings.Add(new Temperature
                    {
                        TimestampUTC = baseTimestamp,
                        AccountId = accountId,
                        DeviceId = deviceId,
                        MessageId = messageId,
                        SensorId = TemperatureProbeSensor?.SensorId,
                        ValueC = Convert.ToDouble(tempDs)
                    });
                }
                
                // Probe Type (Ext)
                if (message.Object.TryGetValue("Ext", out var ext))
                {
                    readings.Add(new ProbeType
                    {
                        TimestampUTC = baseTimestamp,
                        AccountId = accountId,
                        DeviceId = deviceId,
                        MessageId = messageId,
                        SensorId = TemperatureProbeSensor?.SensorId,
                        Value = Convert.ToInt32(ext)
                    });
                }
            }
            
            // fPort 4: Probe ID
            if (fPort == 4)
            {
                if (message.Object.TryGetValue("DS18B20_ID", out var probeId))
                {
                    readings.Add(new ProbeId
                    {
                        TimestampUTC = baseTimestamp,
                        AccountId = accountId,
                        DeviceId = deviceId,
                        MessageId = messageId,
                        SensorId = TemperatureProbeSensor?.SensorId,
                        Value = probeId?.ToString()
                    });
                }
            }
            
            // fPort 5: Battery, Model, Firmware, LoRa Config
            if (fPort == 5)
            {
                // Battery Voltage (Bat_mV)
                if (message.Object.TryGetValue("Bat_mV", out var batMv))
                {
                    readings.Add(new Voltage
                    {
                        TimestampUTC = baseTimestamp,
                        AccountId = accountId,
                        DeviceId = deviceId,
                        MessageId = messageId,
                        SensorId = VoltageSensor?.SensorId,
                        ValueV = Convert.ToDouble(batMv) / 1000.0
                    });
                }
                
                // Device Model (Sensor_Model)
                if (message.Object.TryGetValue("Sensor_Model", out var sensorModel))
                {
                    readings.Add(new ModelNumber
                    {
                        TimestampUTC = baseTimestamp,
                        AccountId = accountId,
                        DeviceId = deviceId,
                        MessageId = messageId,
                        SensorId = DeviceModelSensor?.SensorId,
                        Value = Convert.ToInt32(sensorModel)
                    });
                }
                
                // Firmware Version
                if (message.Object.TryGetValue("Firmware_Version", out var firmwareVersion))
                {
                    readings.Add(new Readings.Version
                    {
                        TimestampUTC = baseTimestamp,
                        AccountId = accountId,
                        DeviceId = deviceId,
                        MessageId = messageId,
                        SensorId = FirmwareSensor?.SensorId,
                        Value = firmwareVersion?.ToString()
                    });
                }
                
                // LoRa Config (Freq_Band + Sub_Band)
                if (message.Object.TryGetValue("Freq_Band", out var freqBand) && 
                    message.Object.TryGetValue("Sub_Band", out var subBand))
                {
                    readings.Add(new LoRaConfig
                    {
                        TimestampUTC = baseTimestamp,
                        AccountId = accountId,
                        DeviceId = deviceId,
                        MessageId = messageId,
                        SensorId = RadioConfigSensor?.SensorId,
                        FrequencyBand = Convert.ToInt32(freqBand),
                        SubBand = Convert.ToInt32(subBand)
                    });
                }
            }
            
            return readings;
        }
    }
}
