using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Messaging.ServiceBus;
using System.Text.Json;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System.Diagnostics;
using NoReturn.LoRaWAN.Devices;
using NoReturn.LoRaWAN.Sensors;
using NoReturn.LoRaWAN.Postgres;

namespace NoReturn.LoRaWAN
{
    public class ServiceBusListener
    {
        private readonly ILogger<ServiceBusListener> _logger;
        private readonly TelemetryClient _telemetryClient;
        private readonly SqlLoRaWANDataProvider _dataProvider;

        public ServiceBusListener(
            ILogger<ServiceBusListener> logger, 
            TelemetryClient telemetryClient,
            SqlLoRaWANDataProvider dataProvider)
        {
            _logger = logger;
            _telemetryClient = telemetryClient;
            _dataProvider = dataProvider;
        }

        [Function(nameof(ServiceBusListener))]
        public async Task Run(
            [ServiceBusTrigger("%ServiceBusQueueName%", Connection = "ServiceBus")] 
            ServiceBusReceivedMessage message)
        {
            var stopwatch = Stopwatch.StartNew();
            
            _logger.LogInformation("ServiceBusListener v2.0 (2026-05-04) - V1 SCHEMA ONLY - Processing message {MessageId}", message.MessageId);
            
            _telemetryClient.TrackEvent("MessageReceiveStarted", new Dictionary<string, string>
            {
                { "MessageId", message.MessageId },
                { "CorrelationId", message.CorrelationId ?? string.Empty },
                { "EnqueuedTimeUtc", message.EnqueuedTime.UtcDateTime.ToString("O") },
                { "DeliveryCount", message.DeliveryCount.ToString() },
                { "SequenceNumber", message.SequenceNumber.ToString() }
            });

            // Deserialize ChirpStack message
            var chirpStackMessage = JsonSerializer.Deserialize<ChirpStackMessage>(message.Body);
            
            if (chirpStackMessage?.DeviceInfo?.DevEui == null)
            {
                throw new Exception("DevEui is missing from message");
            }

            string deviceId = chirpStackMessage.DeviceInfo.DevEui;

            // Get device configuration from v1 schema
            var deviceConfig = await _dataProvider.GetDeviceConfigurationAsync(deviceId);
            
            if (deviceConfig == null)
            {
                throw new Exception($"Device {deviceId} not found in v1 schema");
            }

            // Create device instance using factory based on ChirpStack device profile
            var deviceProfileName = chirpStackMessage.DeviceInfo?.DeviceProfileName;
            if (string.IsNullOrEmpty(deviceProfileName))
            {
                throw new Exception($"DeviceProfileName is missing from ChirpStack message for device {deviceId}");
            }
            
            var device = DeviceFactory.Create(deviceProfileName);
            device.DeviceId = deviceId;
            
            // Populate device sensors from v1 configuration
            foreach (var mapping in deviceConfig.SensorMappings)
            {
                var sensorType = mapping.Key;
                var sensorId = mapping.Value;
                var sensor = SensorFactory.Create(sensorType, sensorId);
                device.Sensors.Add(sensor);
            }
            
            // Decode ChirpStack message into readings (using device-specific codec)
            var readings = device.CreateReadings(
                chirpStackMessage, 
                deviceConfig.AccountId.ToString(), 
                deviceConfig.GroupId.ToString()
            );
            
            // Insert readings to v1 schema
            await _dataProvider.InsertReadingsAsync(deviceConfig, readings);
            
            // Track metrics
            stopwatch.Stop();
            _telemetryClient.TrackMetric("ServiceBusListener_ExecutionMs", stopwatch.ElapsedMilliseconds);
            _telemetryClient.TrackMetric("ReadingsCreated", readings.Count);
            _telemetryClient.TrackMetric("ReadingsInserted", readings.Count);
            
            _telemetryClient.TrackEvent("MessageProcessingCompleted", new Dictionary<string, string>
            {
                { "Status", "Success" },
                { "MessageId", message.MessageId },
                { "DeviceId", deviceId },
                { "AccountId", deviceConfig.AccountId.ToString() },
                { "GroupId", deviceConfig.GroupId.ToString() },
                { "ReadingsCount", readings.Count.ToString() }
            });
        }
    }
}
