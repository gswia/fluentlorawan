using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using NoReturn.LoRaWAN.Graph;

namespace NoReturn.LoRaWAN;

public class Chat2Function
{
    private readonly ILogger<Chat2Function> _logger;
    private readonly AzureOpenAIClient _openAiClient;

    public Chat2Function(
        ILogger<Chat2Function> logger,
        AzureOpenAIClient openAiClient)
    {
        _logger = logger;
        _openAiClient = openAiClient;
    }

    [Function("Chat2")]
    public async Task<HttpResponseData> Chat2(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "chat2")] 
        HttpRequestData req)
    {
        _logger.LogInformation("Chat2 endpoint called");

        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var chatRequest = JsonSerializer.Deserialize<ChatRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (chatRequest == null || string.IsNullOrEmpty(chatRequest.Message))
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("Message is required");
                return badRequest;
            }

            _logger.LogInformation("Received message: {Message}", chatRequest.Message);

            // Build simple test graph
            var graph = GraphStore.BuildSimpleTestGraph();
            _logger.LogInformation("Graph loaded: {SubjectCount} subjects, {SensorCount} sensors", 
                graph.Subjects.Count, graph.Sensors.Count);

            // Build subject list for LLM prompt
            var subjectListText = string.Join("\n", graph.Subjects.Values.Select(s => $"- {s.Id}: {s.Name} (type: {s.Type})"));

            // Step 1: Extract intents and match subjects using LLM
            var deploymentName = Environment.GetEnvironmentVariable("AzureOpenAI__DeploymentName") ?? "gpt-5.4";
            var chatClient = _openAiClient.GetChatClient(deploymentName);

            var extractionPrompt = $@"You are an intent and location extractor for an IoT sensor monitoring system.

Extract the user's intent(s) and match to the correct subjects from the available list below.

Valid intents (use EXACTLY these names):
- TEMPERATURE_CURRENT
- TEMPERATURE_COMPARISON
- HUMIDITY_CURRENT
- DOOR_STATUS
- ILLUMINATION_CURRENT
- VIBRATION_RUNTIME
- ACCELERATION_STATUS
- BATTERY_STATUS
- HOUSE_OVERVIEW

Available subjects in this property:
{subjectListText}

Return a JSON array of objects with:
- 'intent': the intent name
- 'subjectIds': array of subject IDs that match the user's query
- 'timeWindow': PostgreSQL interval format (e.g., ""24 hours"", ""7 days"", ""1 week""), default to ""24 hours""
- 'compareWith': PostgreSQL interval for comparison period (e.g., ""24 hours"" for previous day), or null if no comparison

Matching rules:
- Match shortcuts naturally (""master"" → master_bedroom, ""bed 4"" → bedroom_4)
- Match plural categories (""bedrooms"" → all subjects with type room.bedroom)
- Match hierarchical queries (""rooms"" → all subjects with type starting with room.)
- If no specific location mentioned, match to 'property' subject (whole house)

Time extraction rules:
- ""last 24 hours"", ""today"" → timeWindow: ""24 hours"", compareWith: null
- ""last week"" → timeWindow: ""7 days"", compareWith: null
- ""today vs yesterday"" → timeWindow: ""24 hours"", compareWith: ""24 hours""
- ""this week vs last week"" → timeWindow: ""7 days"", compareWith: ""7 days""
- Default if not specified: timeWindow: ""24 hours"", compareWith: null

Examples:
User: ""How warm is bedroom 4?""
Output: [{{""intent"": ""TEMPERATURE_CURRENT"", ""subjectIds"": [""bedroom_4""], ""timeWindow"": ""24 hours"", ""compareWith"": null}}]

User: ""Temperature in master today vs yesterday?""
Output: [{{""intent"": ""TEMPERATURE_COMPARISON"", ""subjectIds"": [""master_bedroom""], ""timeWindow"": ""24 hours"", ""compareWith"": ""24 hours""}}]

User: ""How warm are the bedrooms last week?""
Output: [{{""intent"": ""TEMPERATURE_CURRENT"", ""subjectIds"": [""master_bedroom"", ""bedroom_4""], ""timeWindow"": ""7 days"", ""compareWith"": null}}]

Now extract from this user message:";

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(extractionPrompt),
                new UserChatMessage(chatRequest.Message)
            };

            var completion = await chatClient.CompleteChatAsync(messages);
            var extractedJson = completion.Value.Content[0].Text;

            // Track token usage
            var usage = completion.Value.Usage;
            var tokenInfo = $"Tokens - In: {usage.InputTokenCount}, Out: {usage.OutputTokenCount}, Total: {usage.TotalTokenCount}";
            _logger.LogInformation("Token usage: {TokenInfo}", tokenInfo);

            _logger.LogInformation("Extracted intents: {Json}", extractedJson);

            // Parse the extracted intents
            var intentPairs = JsonSerializer.Deserialize<List<IntentSubjectPair>>(extractedJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (intentPairs == null || intentPairs.Count == 0)
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("Could not extract intent from message");
                return badRequest;
            }

            // Build execution plan
            var executionPlan = new ExecutionPlan
            {
                Tokens = new TokenUsage
                {
                    InputTokens = usage.InputTokenCount,
                    OutputTokens = usage.OutputTokenCount,
                    TotalTokens = usage.TotalTokenCount
                }
            };

            // Hardcoded context for now
            var accountId = Guid.Parse("00000000-0000-0000-0000-000000000001"); // TODO: Get from auth context
            var timezone = "America/Phoenix"; // TODO: Get from user profile

            foreach (var intentPair in intentPairs)
            {
                _logger.LogInformation("Processing intent: {Intent} with {Count} subject IDs", 
                    intentPair.Intent, intentPair.SubjectIds.Count);

                // Determine capability names (one intent can need multiple capabilities)
                var capabilityNames = intentPair.Intent switch
                {
                    "TEMPERATURE_CURRENT" => new[] { "TemperatureCapability" },
                    "TEMPERATURE_COMPARISON" => new[] { "TemperatureCapability" },
                    "HUMIDITY_CURRENT" => new[] { "HumidityCapability" },
                    "DOOR_STATUS" => new[] { "DoorCapability" },
                    "ILLUMINATION_CURRENT" => new[] { "IlluminationCapability" },
                    "VIBRATION_RUNTIME" => new[] { "VibrationCapability" },
                    "HOUSE_OVERVIEW" => new[] { "TemperatureCapability", "HumidityCapability", "DoorCapability", "IlluminationCapability", "VibrationCapability" },
                    _ => new[] { "Unknown" }
                };

                // Map capabilities to sensor types
                var requiredSensorTypes = new HashSet<SensorType>();
                foreach (var capability in capabilityNames)
                {
                    var sensorType = capability switch
                    {
                        "TemperatureCapability" => SensorType.Temperature,
                        "HumidityCapability" => SensorType.Humidity,
                        "DoorCapability" => SensorType.Door,
                        "IlluminationCapability" => SensorType.Illumination,
                        "VibrationCapability" => SensorType.Vibration,
                        _ => (SensorType?)null
                    };
                    if (sensorType.HasValue)
                    {
                        requiredSensorTypes.Add(sensorType.Value);
                    }
                }

                // Resolve subject IDs to sensor GUIDs, filtered by required sensor types
                var sensorIds = new List<Guid>();
                foreach (var subjectId in intentPair.SubjectIds)
                {
                    if (graph.Subjects.TryGetValue(subjectId, out var subject))
                    {
                        // Get all sensors for this subject via sensor relationships
                        var subjectSensorIds = graph.SubjectSensors
                            .Where(sr => sr.SubjectId == subjectId)
                            .Select(sr => sr.SensorId)
                            .ToList();
                        
                        foreach (var sensorId in subjectSensorIds)
                        {
                            if (graph.Sensors.TryGetValue(sensorId, out var sensor))
                            {
                                // Only add sensors matching required types
                                if (requiredSensorTypes.Contains(sensor.SensorType))
                                {
                                    sensorIds.Add(sensor.SensorId);
                                }
                            }
                        }
                    }
                }

                executionPlan.Steps.Add(new ExecutionStep
                {
                    Intent = intentPair.Intent,
                    SubjectIds = intentPair.SubjectIds,
                    Capabilities = capabilityNames.ToList(),
                    AccountId = accountId,
                    SensorIds = sensorIds,
                    Timezone = timezone,
                    TimeWindow = intentPair.TimeWindow,
                    CompareWith = intentPair.CompareWith
                });
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            await response.WriteStringAsync(JsonSerializer.Serialize(executionPlan, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat2 request");
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync($"Error: {ex.Message}");
            return errorResponse;
        }
    }
}
