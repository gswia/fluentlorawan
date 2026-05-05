using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Messaging.ServiceBus;
using Npgsql;
using NpgsqlTypes;
using System.Text.Json;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System.Diagnostics;
using IotHubFunction.Devices;
using IotHubFunction.Sensors;

namespace IotHubFunction
{
    public class ServiceBusListener
    {
        private readonly ILogger<ServiceBusListener> _logger;
        private readonly TelemetryClient _telemetryClient;

        public ServiceBusListener(ILogger<ServiceBusListener> logger, TelemetryClient telemetryClient)
        {
            _logger = logger;
            _telemetryClient = telemetryClient;
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

            // Connect to PostgreSQL
            var connectionString = Environment.GetEnvironmentVariable("PostgresConnectionString");
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            await using var dataSource = dataSourceBuilder.Build();
            await using var conn = await dataSource.OpenConnectionAsync();
            
            // Query v1 schema for device configuration using function
            await using var v1QueryCmd = new NpgsqlCommand(
                "SELECT * FROM v1.get_device_config($1)",
                conn);
            v1QueryCmd.Parameters.AddWithValue(deviceId);

            var v1Mappings = new Dictionary<string, Guid>(); // sensor_type -> sensor_id
            Guid? v1GroupId = null;
            Guid? v1AccountId = null;

            await using (var reader = await v1QueryCmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    v1GroupId ??= reader.GetGuid(0);
                    v1AccountId ??= reader.GetGuid(1);
                    
                    if (!reader.IsDBNull(2) && !reader.IsDBNull(3))
                    {
                        var sensorId = reader.GetGuid(2);
                        var sensorType = reader.GetString(3);
                        v1Mappings[sensorType] = sensorId;
                    }
                }
            }

            if (v1GroupId == null || v1AccountId == null)
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
            foreach (var mapping in v1Mappings)
            {
                var sensorType = mapping.Key;
                var sensorId = mapping.Value;
                var sensor = SensorFactory.Create(sensorType, sensorId);
                device.Sensors.Add(sensor);
            }
            
            // Decode ChirpStack message into readings (using device-specific codec)
            var readings = device.CreateReadings(
                chirpStackMessage, 
                v1AccountId.Value.ToString(), 
                v1GroupId.Value.ToString()
            );
            
            // Insert readings to v1 schema using functions
            foreach (var reading in readings)
            {
                var payloadJson = JsonSerializer.Serialize(reading.GetPayload());
                
                if (reading is Readings.SensorReading sensorReading)
                {
                    // Map sensor type to v1 sensor_id
                    if (!v1Mappings.TryGetValue(sensorReading.Type, out var v1SensorId))
                    {
                        _logger.LogWarning("Sensor type {SensorType} not configured in v1.sensors for device {DeviceId}, skipping reading", 
                            sensorReading.Type, deviceId);
                        continue;
                    }

                    await using var insertCmd = new NpgsqlCommand(
                        "SELECT v1.insert_sensor_reading($1, $2, $3, $4, $5, $6, $7, $8)",
                        conn);
                    
                    insertCmd.Parameters.AddWithValue(sensorReading.TimestampUTC);
                    insertCmd.Parameters.AddWithValue(v1AccountId.Value);
                    insertCmd.Parameters.AddWithValue(v1GroupId.Value);
                    insertCmd.Parameters.AddWithValue(sensorReading.DeviceId);
                    insertCmd.Parameters.AddWithValue(v1SensorId);
                    insertCmd.Parameters.AddWithValue(Guid.Parse(sensorReading.MessageId));
                    insertCmd.Parameters.AddWithValue(sensorReading.Type);
                    insertCmd.Parameters.AddWithValue(NpgsqlDbType.Jsonb, payloadJson);
                    
                    await insertCmd.ExecuteNonQueryAsync();
                }
                else if (reading is Readings.GatewayReading gatewayReading)
                {
                    await using var insertCmd = new NpgsqlCommand(
                        "SELECT v1.insert_gateway_reading($1, $2, $3, $4, $5, $6, $7, $8)",
                        conn);
                    
                    insertCmd.Parameters.AddWithValue(gatewayReading.TimestampUTC);
                    insertCmd.Parameters.AddWithValue(v1AccountId.Value);
                    insertCmd.Parameters.AddWithValue(v1GroupId.Value);
                    insertCmd.Parameters.AddWithValue(gatewayReading.DeviceId);
                    insertCmd.Parameters.AddWithValue(gatewayReading.GatewayId);
                    insertCmd.Parameters.AddWithValue(Guid.Parse(gatewayReading.MessageId));
                    insertCmd.Parameters.AddWithValue(gatewayReading.Type);
                    insertCmd.Parameters.AddWithValue(NpgsqlDbType.Jsonb, payloadJson);
                    
                    await insertCmd.ExecuteNonQueryAsync();
                }
            }
            
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
                { "AccountId", v1AccountId.Value.ToString() },
                { "GroupId", v1GroupId.Value.ToString() },
                { "ReadingsCount", readings.Count.ToString() }
            });
        }
    }
}
