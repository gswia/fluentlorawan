using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Messaging.ServiceBus;
using Npgsql;
using NpgsqlTypes;
using System.Text.Json;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System.Diagnostics;
using IotHubFunction.Configuration;

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
            
            // DEPLOYMENT TRACE: Confirm v1 schema support is active
            _logger.LogInformation("ServiceBusListener v1.1 (2026-05-01) - V1 SCHEMA ENABLED - Processing message {MessageId}", message.MessageId);
            
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

            // Query PostgreSQL for account using device ID
            var connectionString = Environment.GetEnvironmentVariable("PostgresConnectionString");
            
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            await using var dataSource = dataSourceBuilder.Build();
            await using var conn = await dataSource.OpenConnectionAsync();
            
            // Query for account JSON by device ID
            await using var cmd = new NpgsqlCommand(
                @"SELECT account 
                  FROM public.accounts 
                  WHERE account @> jsonb_build_object(
                      'Applications', jsonb_build_array(
                          jsonb_build_object(
                              'Sites', jsonb_build_array(
                                  jsonb_build_object(
                                      'Devices', jsonb_build_array(
                                          jsonb_build_object('DeviceId', @device_id)
                                      )
                                  )
                              )
                          )
                      )
                  )",
                conn);
            cmd.Parameters.AddWithValue("device_id", deviceId);
            
            var accountJson = await cmd.ExecuteScalarAsync() as string;
            
            if (string.IsNullOrEmpty(accountJson))
            {
                throw new Exception($"Device {deviceId} not found in any account");
            }

            // Deserialize account
            var account = JsonSerializer.Deserialize<Account>(accountJson);
            
            if (account == null)
            {
                throw new Exception("Failed to deserialize account JSON");
            }

            // Find the device in the account hierarchy
            Device? device = null;
            string? applicationId = null;
            string? siteId = null;

            foreach (var application in account.Applications)
            {
                foreach (var site in application.Sites)
                {
                    device = site.Devices.FirstOrDefault(d => d.DeviceId == deviceId);
                    if (device != null)
                    {
                        applicationId = application.ApplicationId;
                        siteId = site.SiteId;
                        break;
                    }
                }
                if (device != null) break;
            }

            if (device == null || applicationId == null || siteId == null)
            {
                throw new Exception($"Device {deviceId} not found in account hierarchy");
            }

            // Create readings
            var readings = device.CreateReadings(chirpStackMessage, account.AccountId, applicationId, siteId);
            
            // Insert readings into PostgreSQL (existing public schema)
            foreach (var reading in readings)
            {
                var payloadJson = JsonSerializer.Serialize(reading.GetPayload());
                
                if (reading is Readings.SensorReading sensorReading)
                {
                    await using var insertCmd = new NpgsqlCommand(
                        @"INSERT INTO sensor_readings (
                            timestamp_utc, account_id, application_id, site_id, 
                            device_id, sensor_id, message_id, type, payload
                        ) VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9)",
                        conn);
                    
                    insertCmd.Parameters.AddWithValue(sensorReading.TimestampUTC);
                    insertCmd.Parameters.AddWithValue(sensorReading.AccountId);
                    insertCmd.Parameters.AddWithValue(sensorReading.ApplicationId);
                    insertCmd.Parameters.AddWithValue(sensorReading.SiteId);
                    insertCmd.Parameters.AddWithValue(sensorReading.DeviceId);
                    insertCmd.Parameters.AddWithValue(sensorReading.SensorId);
                    insertCmd.Parameters.AddWithValue(sensorReading.MessageId);
                    insertCmd.Parameters.AddWithValue(sensorReading.Type);
                    insertCmd.Parameters.AddWithValue(NpgsqlDbType.Jsonb, payloadJson);
                    
                    await insertCmd.ExecuteNonQueryAsync();
                }
                else if (reading is Readings.GatewayReading gatewayReading)
                {
                    await using var insertCmd = new NpgsqlCommand(
                        @"INSERT INTO gateway_readings (
                            timestamp_utc, account_id, application_id, site_id, 
                            device_id, gateway_id, message_id, type, payload
                        ) VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9)",
                        conn);
                    
                    insertCmd.Parameters.AddWithValue(gatewayReading.TimestampUTC);
                    insertCmd.Parameters.AddWithValue(gatewayReading.AccountId);
                    insertCmd.Parameters.AddWithValue(gatewayReading.ApplicationId);
                    insertCmd.Parameters.AddWithValue(gatewayReading.SiteId);
                    insertCmd.Parameters.AddWithValue(gatewayReading.DeviceId);
                    insertCmd.Parameters.AddWithValue(gatewayReading.GatewayId);
                    insertCmd.Parameters.AddWithValue(gatewayReading.MessageId);
                    insertCmd.Parameters.AddWithValue(gatewayReading.Type);
                    insertCmd.Parameters.AddWithValue(NpgsqlDbType.Jsonb, payloadJson);
                    
                    await insertCmd.ExecuteNonQueryAsync();
                }
            }

            // Insert readings into v1 schema (side-by-side, failures won't break existing flow)
            try
            {
                // Query v1 schema for device configuration
                await using var v1QueryCmd = new NpgsqlCommand(
                    @"SELECT 
                        dg.group_id,
                        g.account_id,
                        s.sensor_id,
                        s.sensor_type
                      FROM v1.devices d
                      JOIN v1.device_groups dg ON dg.device_id = d.device_id
                      JOIN v1.groups g ON g.group_id = dg.group_id
                      LEFT JOIN v1.sensors s ON s.device_id = d.device_id
                      WHERE d.device_id = @device_id",
                    conn);
                v1QueryCmd.Parameters.AddWithValue("device_id", deviceId);

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

                if (v1GroupId != null && v1AccountId != null)
                {
                    // Insert readings to v1 schema
                    foreach (var reading in readings)
                {
                    var payloadJson = JsonSerializer.Serialize(reading.GetPayload());
                    
                    if (reading is Readings.SensorReading sensorReading)
                    {
                        // Map sensor type to v1 sensor_id
                        if (!v1Mappings.TryGetValue(sensorReading.Type, out var v1SensorId))
                        {
                            _logger.LogInformation("V1: Sensor type {SensorType} not found in v1.sensors for device {DeviceId}, skipping reading", 
                                sensorReading.Type, deviceId);
                            continue;
                        }

                        await using var insertCmd = new NpgsqlCommand(
                            @"INSERT INTO v1.sensor_readings (
                                timestamp_utc, account_id, group_id, 
                                device_id, sensor_id, message_id, type, payload
                            ) VALUES ($1, $2, $3, $4, $5, $6, $7, $8)",
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
                            @"INSERT INTO v1.gateway_readings (
                                timestamp_utc, account_id, group_id, 
                                device_id, gateway_id, message_id, type, payload
                            ) VALUES ($1, $2, $3, $4, $5, $6, $7, $8)",
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

                    _telemetryClient.TrackEvent("V1SchemaInsertSuccess", new Dictionary<string, string>
                    {
                        { "DeviceId", deviceId },
                        { "ReadingsCount", readings.Count.ToString() },
                        { "GroupId", v1GroupId.Value.ToString() }
                    });
                    
                    _logger.LogInformation("V1: Successfully inserted {ReadingCount} readings to v1 schema for device {DeviceId}, group {GroupId}", 
                        readings.Count, deviceId, v1GroupId.Value);
                }
                else
                {
                    _logger.LogInformation("V1: Device {DeviceId} not found in v1 schema, skipping v1 insert", deviceId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "V1: Failed to insert readings to v1 schema for device {DeviceId}, continuing with existing flow", deviceId);
                _telemetryClient.TrackException(ex, new Dictionary<string, string>
                {
                    { "DeviceId", deviceId },
                    { "Operation", "V1SchemaInsert" }
                });
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
                { "AccountId", account.AccountId },
                { "ReadingsCount", readings.Count.ToString() }
            });
        }
    }
}
