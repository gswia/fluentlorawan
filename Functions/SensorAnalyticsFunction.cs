using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using System.Net;
using System.Text.Json;

namespace NoReturn.LoRaWAN;

public class SensorAnalyticsFunction
{
    private readonly ILogger<SensorAnalyticsFunction> _logger;
    private readonly NpgsqlDataSource _dataSource;

    public SensorAnalyticsFunction(
        ILogger<SensorAnalyticsFunction> logger,
        NpgsqlDataSource dataSource)
    {
        _logger = logger;
        _dataSource = dataSource;
    }

    [Function("GetSensorAnalysis")]
    public async Task<HttpResponseData> GetSensorAnalysis(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sensor-analysis")] 
        HttpRequestData req)
    {
        _logger.LogInformation("GetSensorAnalysis called");

        try
        {
            // Parse request
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonSerializer.Deserialize<AnalysisRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (request == null)
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("Invalid request body");
                return badRequest;
            }

            // Validate required parameters
            if (request.AccountId == Guid.Empty)
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("AccountId is required");
                return badRequest;
            }

            if (string.IsNullOrEmpty(request.Timezone))
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("Timezone is required");
                return badRequest;
            }

            if (string.IsNullOrEmpty(request.CurrentWindow))
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("CurrentWindow is required");
                return badRequest;
            }

            // Call PostgreSQL function
            await using var conn = await _dataSource.OpenConnectionAsync();
            await using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                SELECT * FROM v1.get_timezone_analysis_stats(
                    @account_id,
                    @timezone,
                    @analysis_datetime,
                    @current_window,
                    @group_id,
                    @sensor_types,
                    @previous_window
                )";

            cmd.Parameters.AddWithValue("account_id", request.AccountId);
            cmd.Parameters.AddWithValue("timezone", request.Timezone);
            cmd.Parameters.AddWithValue("analysis_datetime", request.AnalysisDatetime ?? DateTimeOffset.UtcNow);
            cmd.Parameters.AddWithValue("current_window", NpgsqlDbType.Interval, ParseInterval(request.CurrentWindow));
            cmd.Parameters.AddWithValue("group_id", (object?)request.GroupId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("sensor_types", NpgsqlDbType.Array | NpgsqlDbType.Text, (object?)request.SensorTypes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("previous_window", request.PreviousWindow != null ? ParseInterval(request.PreviousWindow) : DBNull.Value);

            await using var reader = await cmd.ExecuteReaderAsync();

            var results = new List<AnalysisResult>();

            while (await reader.ReadAsync())
            {
                var groupId = reader.GetGuid(0);
                var analysisData = reader.GetString(1);

                results.Add(new AnalysisResult
                {
                    GroupId = groupId,
                    AnalysisData = JsonSerializer.Deserialize<JsonElement>(analysisData)
                });
            }

            // Return response
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(new
            {
                Success = true,
                Results = results,
                RequestedAt = DateTimeOffset.UtcNow
            }));

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetSensorAnalysis");
            
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync(JsonSerializer.Serialize(new
            {
                Success = false,
                Error = ex.Message
            }));
            
            return errorResponse;
        }
    }

    [Function("GetSensorAnalysis2")]
    public async Task<HttpResponseData> GetSensorAnalysis2(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "sensor-analysis2")] 
        HttpRequestData req)
    {
        _logger.LogInformation("GetSensorAnalysis2 called");

        try
        {
            // Parse request
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var request = JsonSerializer.Deserialize<AnalysisRequest2>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (request == null)
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("Invalid request body");
                return badRequest;
            }

            // Validate required parameters
            if (request.AccountId == Guid.Empty)
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("AccountId is required");
                return badRequest;
            }

            if (string.IsNullOrEmpty(request.Timezone))
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("Timezone is required");
                return badRequest;
            }

            if (request.SensorIds == null || request.SensorIds.Length == 0)
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("SensorIds array is required and must not be empty");
                return badRequest;
            }

            if (string.IsNullOrEmpty(request.CurrentWindow))
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("CurrentWindow is required");
                return badRequest;
            }

            // Call PostgreSQL function
            await using var conn = await _dataSource.OpenConnectionAsync();
            await using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                SELECT * FROM v1.get_timezone_analysis_stats2(
                    @account_id,
                    @sensor_ids,
                    @timezone,
                    @analysis_datetime,
                    @current_window,
                    @previous_window
                )";

            cmd.Parameters.AddWithValue("account_id", request.AccountId);
            cmd.Parameters.AddWithValue("sensor_ids", NpgsqlDbType.Array | NpgsqlDbType.Uuid, request.SensorIds);
            cmd.Parameters.AddWithValue("timezone", request.Timezone);
            cmd.Parameters.AddWithValue("analysis_datetime", request.AnalysisDatetime ?? DateTimeOffset.UtcNow);
            cmd.Parameters.AddWithValue("current_window", NpgsqlDbType.Interval, ParseInterval(request.CurrentWindow));
            cmd.Parameters.AddWithValue("previous_window", request.PreviousWindow != null ? ParseInterval(request.PreviousWindow) : DBNull.Value);

            await using var reader = await cmd.ExecuteReaderAsync();

            var results = new List<AnalysisResult2>();

            while (await reader.ReadAsync())
            {
                var sensorId = reader.GetGuid(0);
                var analysisData = reader.GetString(1);

                results.Add(new AnalysisResult2
                {
                    SensorId = sensorId,
                    AnalysisData = JsonSerializer.Deserialize<JsonElement>(analysisData)
                });
            }

            // Return response
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(new
            {
                Success = true,
                Results = results,
                RequestedAt = DateTimeOffset.UtcNow
            }));

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetSensorAnalysis2");
            
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync(JsonSerializer.Serialize(new
            {
                Success = false,
                Error = ex.Message
            }));
            
            return errorResponse;
        }
    }

    private static TimeSpan ParseInterval(string interval)
    {
        // Parse strings like "24 hours", "7 days", "30 minutes"
        var parts = interval.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !int.TryParse(parts[0], out var value))
        {
            throw new ArgumentException($"Invalid interval format: {interval}. Expected format like '24 hours' or '7 days'");
        }

        var unit = parts[1].ToLower().TrimEnd('s'); // Handle plural
        return unit switch
        {
            "second" => TimeSpan.FromSeconds(value),
            "minute" => TimeSpan.FromMinutes(value),
            "hour" => TimeSpan.FromHours(value),
            "day" => TimeSpan.FromDays(value),
            "week" => TimeSpan.FromDays(value * 7),
            _ => throw new ArgumentException($"Unknown time unit: {parts[1]}")
        };
    }
}

// Request model (v1 - group-based)
public class AnalysisRequest
{
    public Guid AccountId { get; set; }
    public string Timezone { get; set; } = string.Empty;
    public DateTimeOffset? AnalysisDatetime { get; set; }
    public string CurrentWindow { get; set; } = string.Empty;
    public Guid? GroupId { get; set; }
    public string[]? SensorTypes { get; set; }
    public string? PreviousWindow { get; set; }
}

// Response model (v1 - group-based)
public class AnalysisResult
{
    public Guid GroupId { get; set; }
    public JsonElement AnalysisData { get; set; }
}

// Request model (v2 - sensor-based for Graph RAG)
public class AnalysisRequest2
{
    public string Timezone { get; set; } = string.Empty;
    public Guid AccountId { get; set; }
    public Guid[] SensorIds { get; set; } = Array.Empty<Guid>();
    public DateTimeOffset? AnalysisDatetime { get; set; }
    public string CurrentWindow { get; set; } = string.Empty;
    public string? PreviousWindow { get; set; }
}

// Response model (v2 - sensor-based for Graph RAG)
public class AnalysisResult2
{
    public Guid SensorId { get; set; }
    public JsonElement AnalysisData { get; set; }
}
