using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Messaging.ServiceBus;
using Azure.Identity;
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
            var managedIdentityClientId = Environment.GetEnvironmentVariable("ManagedIdentityClientId");
            
            var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = managedIdentityClientId
            });
            
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
            dataSourceBuilder.UsePeriodicPasswordProvider(
                async (_, ct) =>
                {
                    var token = await credential.GetTokenAsync(
                        new Azure.Core.TokenRequestContext(new[] { "https://ossrdbms-aad.database.windows.net/.default" }),
                        ct
                    );
                    return token.Token;
                },
                TimeSpan.FromHours(1),
                TimeSpan.FromSeconds(10)
            );
            
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
            
            // Insert readings into PostgreSQL
            foreach (var reading in readings)
            {
                await using var insertCmd = new NpgsqlCommand(
                    @"INSERT INTO readings (
                        timestamp_utc, account_id, application_id, site_id, 
                        device_id, sensor_id, message_id, type, payload
                    ) VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9)",
                    conn);
                
                var payloadJson = JsonSerializer.Serialize(reading.GetPayload());
                
                insertCmd.Parameters.AddWithValue(reading.TimestampUTC);
                insertCmd.Parameters.AddWithValue(reading.AccountId);
                insertCmd.Parameters.AddWithValue(reading.ApplicationId);
                insertCmd.Parameters.AddWithValue(reading.SiteId);
                insertCmd.Parameters.AddWithValue(reading.DeviceId);
                insertCmd.Parameters.AddWithValue(reading.SensorId);
                insertCmd.Parameters.AddWithValue(reading.MessageId);
                insertCmd.Parameters.AddWithValue(reading.Type);
                insertCmd.Parameters.AddWithValue(NpgsqlDbType.Jsonb, payloadJson);
                
                await insertCmd.ExecuteNonQueryAsync();
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
