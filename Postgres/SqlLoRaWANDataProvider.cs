using Microsoft.Extensions.Logging;
using NoReturn.LoRaWAN.Readings;
using Npgsql;
using NpgsqlTypes;
using System.Text.Json;

namespace NoReturn.LoRaWAN.Postgres;

/// <summary>
/// SQL provider for LoRaWAN device data using PostgreSQL v1 schema functions
/// </summary>
public class SqlLoRaWANDataProvider : IDisposable
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<SqlLoRaWANDataProvider> _logger;

    public SqlLoRaWANDataProvider(string connectionString, ILogger<SqlLoRaWANDataProvider> logger)
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        _dataSource = dataSourceBuilder.Build();
        _logger = logger;
    }

    public async Task<DeviceConfiguration?> GetDeviceConfigurationAsync(
        string deviceId, 
        CancellationToken cancellationToken = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var cmd = new NpgsqlCommand("SELECT * FROM v1.get_device_config($1)", conn);
        cmd.Parameters.AddWithValue(deviceId);

        var sensorMappings = new Dictionary<string, Guid>();
        Guid? groupId = null;
        Guid? accountId = null;

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            groupId ??= reader.GetGuid(0);
            accountId ??= reader.GetGuid(1);

            if (!reader.IsDBNull(2) && !reader.IsDBNull(3))
            {
                var sensorId = reader.GetGuid(2);
                var sensorType = reader.GetString(3);
                sensorMappings[sensorType] = sensorId;
            }
        }

        if (groupId == null || accountId == null)
        {
            _logger.LogDebug("Device {DeviceId} not found in v1 schema", deviceId);
            return null;
        }

        return new DeviceConfiguration
        {
            AccountId = accountId.Value,
            GroupId = groupId.Value,
            SensorMappings = sensorMappings
        };
    }

    public async Task InsertReadingsAsync(
        DeviceConfiguration deviceConfig,
        IReadOnlyList<Reading> readings,
        CancellationToken cancellationToken = default)
    {
        await using var conn = await _dataSource.OpenConnectionAsync(cancellationToken);

        foreach (var reading in readings)
        {
            var payloadJson = JsonSerializer.Serialize(reading.GetPayload());

            if (reading is SensorReading sensorReading)
            {
                // Check if sensor type exists in mappings
                if (!deviceConfig.TryGetSensorId(sensorReading.Type, out var sensorId))
                {
                    _logger.LogDebug(
                        "Sensor type {SensorType} not found for device {DeviceId}, skipping",
                        sensorReading.Type,
                        sensorReading.DeviceId);
                    continue;
                }

                await using var insertCmd = new NpgsqlCommand(
                    "SELECT v1.insert_sensor_reading($1, $2, $3, $4, $5, $6, $7, $8)",
                    conn);

                insertCmd.Parameters.AddWithValue(sensorReading.TimestampUTC);
                insertCmd.Parameters.AddWithValue(deviceConfig.AccountId);
                insertCmd.Parameters.AddWithValue(deviceConfig.GroupId);
                insertCmd.Parameters.AddWithValue(sensorReading.DeviceId);
                insertCmd.Parameters.AddWithValue(sensorId);
                insertCmd.Parameters.AddWithValue(Guid.Parse(sensorReading.MessageId));
                insertCmd.Parameters.AddWithValue(sensorReading.Type);
                insertCmd.Parameters.AddWithValue(NpgsqlDbType.Jsonb, payloadJson);

                await insertCmd.ExecuteNonQueryAsync(cancellationToken);
            }
            else if (reading is GatewayReading gatewayReading)
            {
                await using var insertCmd = new NpgsqlCommand(
                    "SELECT v1.insert_gateway_reading($1, $2, $3, $4, $5, $6, $7, $8)",
                    conn);

                insertCmd.Parameters.AddWithValue(gatewayReading.TimestampUTC);
                insertCmd.Parameters.AddWithValue(deviceConfig.AccountId);
                insertCmd.Parameters.AddWithValue(deviceConfig.GroupId);
                insertCmd.Parameters.AddWithValue(gatewayReading.DeviceId);
                insertCmd.Parameters.AddWithValue(gatewayReading.GatewayId);
                insertCmd.Parameters.AddWithValue(Guid.Parse(gatewayReading.MessageId));
                insertCmd.Parameters.AddWithValue(gatewayReading.Type);
                insertCmd.Parameters.AddWithValue(NpgsqlDbType.Jsonb, payloadJson);

                await insertCmd.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        _logger.LogDebug(
            "Inserted {ReadingCount} readings for device in group {GroupId}",
            readings.Count,
            deviceConfig.GroupId);
    }

    public void Dispose()
    {
        _dataSource?.Dispose();
    }
}
