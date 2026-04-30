using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Messaging.ServiceBus;
using Microsoft.ApplicationInsights;

namespace IotHubFunction
{
    public class TrackerUplink
    {
        private readonly ILogger<TrackerUplink> _logger;
        private readonly TelemetryClient _telemetryClient;

        public TrackerUplink(ILogger<TrackerUplink> logger, TelemetryClient telemetryClient)
        {
            _logger = logger;
            _telemetryClient = telemetryClient;
        }

        [Function(nameof(TrackerUplink))]
        public void Run(
            [ServiceBusTrigger("%TrackerUplinkQueueName%", Connection = "ServiceBus")]
            ServiceBusReceivedMessage message)
        {
            var topic = message.ApplicationProperties.TryGetValue("topic", out var t) ? t?.ToString() : "unknown";
            var bytes = message.Body.ToArray();
            var payloadHex = Convert.ToHexString(bytes);
            var payloadStr = System.Text.Encoding.UTF8.GetString(bytes);

            _telemetryClient.TrackEvent("TrackerUplinkReceived", new Dictionary<string, string>
            {
                { "Topic", topic ?? string.Empty },
                { "MessageId", message.MessageId },
                { "Bytes", message.Body.Length.ToString() },
                { "Payload", payloadStr },
                { "PayloadHex", payloadHex }
            });
        }
    }
}
