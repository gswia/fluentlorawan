using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Messaging.ServiceBus;
using Microsoft.ApplicationInsights;

namespace IotHubFunction
{
    public class EelinkUplink
    {
        private readonly ILogger<EelinkUplink> _logger;
        private readonly TelemetryClient _telemetryClient;

        public EelinkUplink(ILogger<EelinkUplink> logger, TelemetryClient telemetryClient)
        {
            _logger = logger;
            _telemetryClient = telemetryClient;
        }

        [Function(nameof(EelinkUplink))]
        public void Run(
            [ServiceBusTrigger("%EelinkUplinkQueueName%", Connection = "ServiceBus")]
            ServiceBusReceivedMessage message)
        {
            var imei = message.ApplicationProperties.TryGetValue("imei", out var i) ? i?.ToString() : "unknown";
            var packetType = message.ApplicationProperties.TryGetValue("packet_type", out var p) ? p?.ToString() : "unknown";
            var bytes = message.Body.ToArray();
            var payloadHex = Convert.ToHexString(bytes);
            var payloadStr = System.Text.Encoding.Latin1.GetString(bytes);

            _logger.LogInformation("Eelink packet type=0x{Type} imei={IMEI} bytes={Bytes} hex={Hex}",
                packetType, imei, bytes.Length, payloadHex);

            _telemetryClient.TrackEvent("EelinkUplinkReceived", new Dictionary<string, string>
            {
                { "IMEI", imei ?? string.Empty },
                { "PacketType", packetType ?? string.Empty },
                { "MessageId", message.MessageId },
                { "Bytes", bytes.Length.ToString() },
                { "Payload", payloadStr },
                { "PayloadHex", payloadHex }
            });
        }
    }
}
