using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Azure.Messaging.ServiceBus;
using Azure.Identity;
using Npgsql;
using NpgsqlTypes;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Ingest;
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
            string deviceId = null;
            string devEui = null;
            string deviceProfileName = null;
            string gatewayId = null;
            int? rssi = null;
            double? snr = null;
            int? fcnt = null;
            Guid? accountId = null;
            
            try
            {
                // Deserialize ChirpStack message
                var chirpStackMessage = JsonSerializer.Deserialize<ChirpStackMessage>(message.Body);
                
                // Create device based on profile name
                var device = DeviceFactory.Create(chirpStackMessage.DeviceInfo?.DeviceProfileName);
                
                // Create readings from message (TODO: need accountId from DB first)
                // var readings = device.CreateReadings(chirpStackMessage, accountId.ToString());
                
                // Build enriched state with all metadata
                var enrichedState = EnrichedDeviceState.FromChirpStackMessage(chirpStackMessage);
                
                // Track latency metrics - total and stages
                var now = DateTime.UtcNow;
                _telemetryClient.TrackMetric("MessageLatency_Total", (now - enrichedState.GwTime).TotalMilliseconds);
                _telemetryClient.TrackMetric("MessageLatency_GatewayToNetworkServer", (enrichedState.NsTime - enrichedState.GwTime).TotalMilliseconds);
                _telemetryClient.TrackMetric("MessageLatency_NetworkServerToServiceBus", (enrichedState.Time - enrichedState.NsTime).TotalMilliseconds);
                _telemetryClient.TrackMetric("MessageLatency_ServiceBusToFunction", (now - enrichedState.Time).TotalMilliseconds);
                
                // Query PostgreSQL for account ID using Managed Identity
                deviceId = enrichedState.DevEui;
                devEui = enrichedState.DevEui;
                deviceProfileName = enrichedState.DeviceProfileName;
                gatewayId = enrichedState.GatewayId;
                rssi = enrichedState.Rssi;
                snr = enrichedState.Snr;
                fcnt = enrichedState.FCnt;
                
                var connectionString = Environment.GetEnvironmentVariable("PostgresConnectionString");
                
                // Setup connection with auto-refreshing token using specific managed identity
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
                
                await using var cmd = new NpgsqlCommand("SELECT get_account_by_device(@p_device_id)", conn);
                cmd.Parameters.AddWithValue("p_device_id", deviceId);
                
                var dbQueryStart = Stopwatch.StartNew();
                accountId = await cmd.ExecuteScalarAsync() as Guid?;
                dbQueryStart.Stop();
                
                _telemetryClient.TrackMetric("Database_AccountQueryMs", dbQueryStart.ElapsedMilliseconds);

                // Serialize ONLY sensor readings to JSON for storage
                var sensorReadingsJson = JsonSerializer.Serialize(enrichedState.SensorReadings);

                // Upsert current device state
                await using var upsertCmd = new NpgsqlCommand(
                    @"SELECT upsert_device_state(
                        @p_device_id::text, 
                        @p_timestamp::timestamp, 
                        @p_device_profile_name::text,
                        @p_fcnt::integer,
                        @p_fport::integer,
                        @p_dr::integer,
                        @p_data::text,
                        @p_gateway_id::text,
                        @p_rssi::integer,
                        @p_snr::double precision,
                        @p_gateway_lat::double precision,
                        @p_gateway_lon::double precision,
                        @p_state::jsonb
                    )", 
                    conn);
                
                upsertCmd.Parameters.AddWithValue("p_device_id", deviceId);
                upsertCmd.Parameters.AddWithValue("p_timestamp", enrichedState.Time);
                upsertCmd.Parameters.AddWithValue("p_device_profile_name", enrichedState.DeviceProfileName ?? (object)DBNull.Value);
                upsertCmd.Parameters.AddWithValue("p_fcnt", enrichedState.FCnt);
                upsertCmd.Parameters.AddWithValue("p_fport", enrichedState.FPort);
                upsertCmd.Parameters.AddWithValue("p_dr", enrichedState.Dr);
                upsertCmd.Parameters.AddWithValue("p_data", enrichedState.Data ?? (object)DBNull.Value);
                upsertCmd.Parameters.AddWithValue("p_gateway_id", enrichedState.GatewayId ?? (object)DBNull.Value);
                upsertCmd.Parameters.AddWithValue("p_rssi", enrichedState.Rssi);
                upsertCmd.Parameters.AddWithValue("p_snr", enrichedState.Snr);
                upsertCmd.Parameters.AddWithValue("p_gateway_lat", enrichedState.GatewayLat ?? (object)DBNull.Value);
                upsertCmd.Parameters.AddWithValue("p_gateway_lon", enrichedState.GatewayLon ?? (object)DBNull.Value);
                upsertCmd.Parameters.AddWithValue("p_state", NpgsqlDbType.Jsonb, sensorReadingsJson);
                
                var upsertStart = Stopwatch.StartNew();
                await upsertCmd.ExecuteNonQueryAsync();
                upsertStart.Stop();
                
                _telemetryClient.TrackMetric("Database_StateUpsertMs", upsertStart.ElapsedMilliseconds);

                // Insert device state history
                await using var insertCmd = new NpgsqlCommand(
                    @"SELECT insert_device_state_history(
                        @p_device_id::text,
                        @p_account_id::uuid,
                        @p_timestamp::timestamp,
                        @p_device_profile_name::text,
                        @p_fcnt::integer,
                        @p_fport::integer,
                        @p_dr::integer,
                        @p_data::text,
                        @p_gateway_id::text,
                        @p_rssi::integer,
                        @p_snr::double precision,
                        @p_gateway_lat::double precision,
                        @p_gateway_lon::double precision,
                        @p_state::jsonb
                    )", 
                    conn);
                
                insertCmd.Parameters.AddWithValue("p_device_id", deviceId);
                insertCmd.Parameters.AddWithValue("p_account_id", accountId ?? (object)DBNull.Value);
                insertCmd.Parameters.AddWithValue("p_timestamp", enrichedState.Time);
                insertCmd.Parameters.AddWithValue("p_device_profile_name", enrichedState.DeviceProfileName ?? (object)DBNull.Value);
                insertCmd.Parameters.AddWithValue("p_fcnt", enrichedState.FCnt);
                insertCmd.Parameters.AddWithValue("p_fport", enrichedState.FPort);
                insertCmd.Parameters.AddWithValue("p_dr", enrichedState.Dr);
                insertCmd.Parameters.AddWithValue("p_data", enrichedState.Data ?? (object)DBNull.Value);
                insertCmd.Parameters.AddWithValue("p_gateway_id", enrichedState.GatewayId ?? (object)DBNull.Value);
                insertCmd.Parameters.AddWithValue("p_rssi", enrichedState.Rssi);
                insertCmd.Parameters.AddWithValue("p_snr", enrichedState.Snr);
                insertCmd.Parameters.AddWithValue("p_gateway_lat", enrichedState.GatewayLat ?? (object)DBNull.Value);
                insertCmd.Parameters.AddWithValue("p_gateway_lon", enrichedState.GatewayLon ?? (object)DBNull.Value);
                insertCmd.Parameters.AddWithValue("p_state", NpgsqlDbType.Jsonb, sensorReadingsJson);
                
                var insertStart = Stopwatch.StartNew();
                await insertCmd.ExecuteNonQueryAsync();
                insertStart.Stop();
                
                _telemetryClient.TrackMetric("Database_HistoryInsertMs", insertStart.ElapsedMilliseconds);
                
                // Track overall processing completion
                stopwatch.Stop();
                _telemetryClient.TrackMetric("ServiceBusListener_ExecutionMs", stopwatch.ElapsedMilliseconds);
                
                _telemetryClient.TrackEvent("MessageProcessingCompleted", new Dictionary<string, string>
                {
                    { "Status", "Success" },
                    { "MessageId", message.MessageId },
                    { "DeviceId", deviceId },
                    { "GatewayId", enrichedState.GatewayId ?? "null" },
                    { "Rssi", rssi?.ToString() ?? "null" },
                    { "Snr", snr?.ToString() ?? "null" },
                    { "FCnt", fcnt?.ToString() ?? "null" },
                    { "AccountId", accountId?.ToString() ?? "null" }
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                _telemetryClient.TrackEvent("MessageProcessingCompleted", new Dictionary<string, string>
                {
                    { "Status", "Failed" },
                    { "MessageId", message.MessageId },
                    { "DeviceId", deviceId ?? "unknown" },
                    { "GatewayId", gatewayId ?? "null" },
                    { "Rssi", rssi?.ToString() ?? "null" },
                    { "Snr", snr?.ToString() ?? "null" },
                    { "FCnt", fcnt?.ToString() ?? "null" },
                    { "AccountId", accountId?.ToString() ?? "null" },
                    { "ErrorType", ex.GetType().Name },
                    { "ErrorMessage", ex.Message }
                });
                
                _telemetryClient.TrackException(ex);
                throw;
            }

            // Send historical state to Azure Data Explorer
            // await SendToAdxAsync(deviceId, accountId, enrichedState.Time, sensorReadingsJson);
        }

        private async Task SendToAdxAsync(
            string deviceId,
            Guid? accountId,
            DateTime timestamp,
            string stateJson)
        {
            try
            {
                var clusterUri = Environment.GetEnvironmentVariable("AdxClusterUri");
                var databaseName = Environment.GetEnvironmentVariable("AdxDatabaseName");
                var managedIdentityClientId = Environment.GetEnvironmentVariable("ManagedIdentityClientId");

                if (string.IsNullOrEmpty(clusterUri) || string.IsNullOrEmpty(databaseName))
                {
                    return;
                }

                // Create Kusto connection with User-Assigned Managed Identity
                var kcsb = new KustoConnectionStringBuilder(clusterUri)
                    .WithAadUserManagedIdentity(managedIdentityClientId);

                using var ingestClient = KustoIngestFactory.CreateQueuedIngestClient(kcsb);

                // Create JSON object for ingestion
                var jsonRecord = new
                {
                    device_id = deviceId,
                    account_id = accountId?.ToString() ?? "",
                    timestamp = timestamp,
                    state = System.Text.Json.JsonDocument.Parse(stateJson).RootElement
                };

                var jsonData = System.Text.Json.JsonSerializer.Serialize(jsonRecord);

                // Ingest properties - hardcoded table name DeviceStates
                var ingestionProperties = new KustoQueuedIngestionProperties(databaseName, "DeviceStates")
                {
                    Format = DataSourceFormat.json
                };

                // Ingest from string stream
                using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonData));
                await ingestClient.IngestFromStreamAsync(stream, ingestionProperties);
            }
            catch (Exception ex)
            {
                // Silently fail ADX ingestion
            }
        }
    }
}
