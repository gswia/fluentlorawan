using System.Text.Json.Serialization;

namespace IotHubFunction
{
    /// <summary>
    /// Minimal ChirpStack message model - only fields we actually use
    /// </summary>
    public class ChirpStackMessage
    {
        [JsonPropertyName("time")]
        public DateTime Time { get; set; }

        [JsonPropertyName("deviceInfo")]
        public DeviceInfo DeviceInfo { get; set; }

        [JsonPropertyName("dr")]
        public int Dr { get; set; }

        [JsonPropertyName("fCnt")]
        public int FCnt { get; set; }

        [JsonPropertyName("fPort")]
        public int FPort { get; set; }

        [JsonPropertyName("data")]
        public string Data { get; set; }

        [JsonPropertyName("object")]
        public Dictionary<string, object> Object { get; set; }

        [JsonPropertyName("rxInfo")]
        public List<RxInfo> RxInfo { get; set; }
    }

    public class DeviceInfo
    {
        [JsonPropertyName("deviceProfileName")]
        public string DeviceProfileName { get; set; }

        [JsonPropertyName("devEui")]
        public string DevEui { get; set; }
    }

    public class RxInfo
    {
        [JsonPropertyName("gatewayId")]
        public string GatewayId { get; set; }

        [JsonPropertyName("gwTime")]
        public DateTime GwTime { get; set; }

        [JsonPropertyName("nsTime")]
        public DateTime NsTime { get; set; }

        [JsonPropertyName("rssi")]
        public int Rssi { get; set; }

        [JsonPropertyName("snr")]
        public double Snr { get; set; }

        [JsonPropertyName("location")]
        public Location Location { get; set; }
    }

    public class Location
    {
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }
    }

    /// <summary>
    /// Enriched state that combines sensor readings with metadata
    /// </summary>
    public class EnrichedDeviceState
    {
        // Message identifiers
        public DateTime Time { get; set; }
        public string DevEui { get; set; }

        // LoRaWAN metadata
        public string DeviceProfileName { get; set; }
        public int FCnt { get; set; }
        public int FPort { get; set; }
        public int Dr { get; set; }
        public string Data { get; set; }

        // Gateway/connectivity
        public string GatewayId { get; set; }
        public int Rssi { get; set; }
        public double Snr { get; set; }
        public double? GatewayLat { get; set; }
        public double? GatewayLon { get; set; }
        public DateTime GwTime { get; set; }
        public DateTime NsTime { get; set; }

        // Sensor readings (dynamic - from object element)
        [JsonExtensionData]
        public Dictionary<string, object> SensorReadings { get; set; }

        public static EnrichedDeviceState FromChirpStackMessage(ChirpStackMessage message)
        {
            var rxInfo = message.RxInfo?.FirstOrDefault();
            
            return new EnrichedDeviceState
            {
                // Message identifiers
                Time = message.Time,
                DevEui = message.DeviceInfo?.DevEui,

                // LoRaWAN metadata
                DeviceProfileName = message.DeviceInfo?.DeviceProfileName,
                FCnt = message.FCnt,
                FPort = message.FPort,
                Dr = message.Dr,
                Data = message.Data,

                // Gateway/connectivity
                GatewayId = rxInfo?.GatewayId,
                Rssi = rxInfo?.Rssi ?? 0,
                Snr = rxInfo?.Snr ?? 0,
                GatewayLat = rxInfo?.Location?.Latitude,
                GatewayLon = rxInfo?.Location?.Longitude,
                GwTime = rxInfo?.GwTime ?? DateTime.MinValue,
                NsTime = rxInfo?.NsTime ?? DateTime.MinValue,

                // Sensor readings - copy from object
                SensorReadings = message.Object ?? new Dictionary<string, object>()
            };
        }
    }
}
