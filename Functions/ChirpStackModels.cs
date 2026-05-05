using System.Text.Json.Serialization;

namespace NoReturn.LoRaWAN
{
    /// <summary>
    /// Minimal ChirpStack message model - only fields we actually use
    /// </summary>
    public class ChirpStackMessage
    {
        [JsonPropertyName("deduplicationId")]
        public string DeduplicationId { get; set; }

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
}
