using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Azure.AI.OpenAI;
using OpenAI.Chat;

namespace NoReturn.LoRaWAN;

public class ChatFunction
{
    private readonly ILogger<ChatFunction> _logger;
    private readonly AzureOpenAIClient _openAiClient;
    private readonly IHttpClientFactory _httpClientFactory;

    public ChatFunction(
        ILogger<ChatFunction> logger,
        AzureOpenAIClient openAiClient,
        IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _openAiClient = openAiClient;
        _httpClientFactory = httpClientFactory;
    }

    [Function("Chat")]
    public async Task<HttpResponseData> Chat(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "chat")] 
        HttpRequestData req)
    {
        _logger.LogInformation("Chat endpoint called");

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

            // Hardcoded defaults for testing
            var accountId = "8f4a2e7c-5b1d-4f89-a3c6-9d8e7f6a5b4c";
            var groupId = "3c8f9a2e-7d4b-4e1f-9a5c-2b8e7f6a9d3c";
            var timezone = "America/Phoenix";

            var deploymentName = Environment.GetEnvironmentVariable("AzureOpenAI__DeploymentName") ?? "gpt-4o";
            var chatClient = _openAiClient.GetChatClient(deploymentName);

            // Define the sensor analysis tool
            var tools = new List<ChatTool>
            {
                ChatTool.CreateFunctionTool(
                    functionName: "get_sensor_analysis",
                    functionDescription: "Get statistical analysis of sensor data for a specific timezone and time window. Returns sensor readings with statistics like min, max, average values. Always uses the default group and account.",
                    functionParameters: BinaryData.FromObjectAsJson(new
                    {
                        type = "object",
                        properties = new
                        {
                            currentWindow = new
                            {
                                type = "string",
                                description = "Time window to analyze. Format: '<number> <unit>' where unit is one of: seconds, minutes, hours, days, weeks. Examples: '24 hours', '7 days', '2 weeks', '30 minutes'"
                            },
                            previousWindow = new
                            {
                                type = "string",
                                description = "Optional: previous time window for comparison. Format: '<number> <unit>' where unit is one of: seconds, minutes, hours, days, weeks. Examples: '24 hours', '7 days'"
                            },
                            sensorTypes = new
                            {
                                type = "array",
                                items = new 
                                { 
                                    type = "string",
                                    @enum = new[] { "Temperature", "Humidity", "Door", "Illumination", "Vibration", "Acceleration", "Voltage" }
                                },
                                description = "Optional: filter to specific sensor types. Valid types: Temperature, Humidity, Door, Illumination, Vibration, Acceleration, Voltage"
                            }
                        },
                        required = new[] { "currentWindow" }
                    })
                )
            };

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage($@"You are a sensor data analyst assistant. 
Current time: {DateTimeOffset.UtcNow:O}
Account: {accountId}
Group: {groupId}
Timezone: {timezone}

You analyze sensor data from IoT devices. Use the get_sensor_analysis tool to retrieve data.

SENSOR TYPE MAPPING (what measures what):
- Temperature → warmth, cold, hot, cool, temperature, climate, heat
- Humidity → moisture, humidity, damp, dry, condensation
- Door → open/closed, entry, access, door status, locked/unlocked
- Illumination → light, brightness, dark, lighting, lux
- Vibration → movement, shaking, vibration, activity (NOT stability - use Acceleration for that)
- Acceleration → stability, tilt, orientation, g-force, movement intensity
- Voltage → battery, power, charge level

QUERY INTERPRETATION RULES:
1. ""warm/hot/cold"" → Temperature sensors
2. ""stable/unstable/tilted"" → Acceleration sensors (NOT Vibration or Illumination)
3. ""movement/activity"" → Vibration OR Acceleration (use both if unclear)
4. ""open/closed"" → Door sensors
5. ""bright/dark"" → Illumination sensors
6. ""battery"" → Voltage sensors
7. If room name mentioned but no sensor type → DO NOT filter sensorTypes, let API return all available

IMPORTANT: Time windows must use these units only: seconds, minutes, hours, days, weeks
Examples: '24 hours', '7 days', '2 weeks', '30 minutes'
For approximate conversions: 1 month ≈ 30 days, 1 year ≈ 52 weeks

When unsure which sensor type to use, omit sensorTypes filter to get all available sensors."),
                new UserChatMessage(chatRequest.Message)
            };

            var options = new ChatCompletionOptions();
            foreach (var tool in tools)
                options.Tools.Add(tool);

            var completion = await chatClient.CompleteChatAsync(messages, options);

            // Check if tool call is needed
            if (completion.Value.FinishReason == ChatFinishReason.ToolCalls)
            {
                _logger.LogInformation("Tool calls requested: {Count}", completion.Value.ToolCalls.Count);
                
                // CRITICAL: Add assistant message FIRST, before handling any tool calls
                messages.Add(new AssistantChatMessage(completion.Value));

                object? lastToolParameters = null;

                // Then add a ToolMessage for EACH tool call
                foreach (var toolCall in completion.Value.ToolCalls)
                {
                    _logger.LogInformation("Processing tool call: {ToolCallId}, Function: {FunctionName}", 
                        toolCall.Id, toolCall.FunctionName);

                    string apiResult;

                    if (toolCall.FunctionName == "get_sensor_analysis")
                    {
                        object? analyticsRequest = null;

                        try
                        {
                            // Parse function arguments
                            var functionArgs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                                toolCall.FunctionArguments.ToString());

                            // Always use hardcoded account, group, timezone
                            analyticsRequest = new
                            {
                                AccountId = accountId,
                                Timezone = timezone,
                                CurrentWindow = functionArgs!["currentWindow"].GetString(),
                                PreviousWindow = functionArgs.ContainsKey("previousWindow") ? functionArgs["previousWindow"].GetString() : null,
                                GroupId = groupId,
                                SensorTypes = functionArgs.ContainsKey("sensorTypes") 
                                    ? functionArgs["sensorTypes"].EnumerateArray().Select(e => e.GetString()).ToArray() 
                                    : null
                            };

                            lastToolParameters = analyticsRequest;

                            _logger.LogInformation("Calling analytics API with request: {Request}", 
                                JsonSerializer.Serialize(analyticsRequest));

                            // Call the sensor analytics API
                            var httpClient = _httpClientFactory.CreateClient();
                            var apiResponse = await httpClient.PostAsJsonAsync(
                                "https://fluentcor-fa.azurewebsites.net/api/sensor-analysis",
                                analyticsRequest
                            );

                            apiResult = await apiResponse.Content.ReadAsStringAsync();
                            
                            if (!apiResponse.IsSuccessStatusCode)
                            {
                                _logger.LogError("Analytics API returned {StatusCode}: {Response}", 
                                    apiResponse.StatusCode, apiResult);
                                apiResult = JsonSerializer.Serialize(new
                                {
                                    error = $"API returned {apiResponse.StatusCode}",
                                    details = apiResult
                                });
                            }
                            else
                            {
                                _logger.LogInformation("Analytics API succeeded");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error calling analytics API");
                            apiResult = JsonSerializer.Serialize(new
                            {
                                error = "Failed to call analytics API",
                                message = ex.Message
                            });
                        }
                    }
                    else
                    {
                        // Unknown function
                        _logger.LogWarning("Unknown tool function: {FunctionName}", toolCall.FunctionName);
                        apiResult = JsonSerializer.Serialize(new
                        {
                            error = "Unknown function",
                            function = toolCall.FunctionName
                        });
                    }

                    // CRITICAL: Add ToolMessage for this tool call
                    messages.Add(new ToolChatMessage(toolCall.Id, apiResult));
                    _logger.LogInformation("Added tool message for {ToolCallId}", toolCall.Id);
                }

                // Now get final response after ALL tool messages are added
                var finalCompletion = await chatClient.CompleteChatAsync(messages);
                
                var response = req.CreateResponse(HttpStatusCode.OK);
                response.Headers.Add("Content-Type", "application/json");
                await response.WriteStringAsync(JsonSerializer.Serialize(new
                {
                    Message = finalCompletion.Value.Content[0].Text,
                    ToolUsed = "get_sensor_analysis",
                    ToolParameters = lastToolParameters
                }));
                return response;
            }

            // No tool call needed
            var simpleResponse = req.CreateResponse(HttpStatusCode.OK);
            simpleResponse.Headers.Add("Content-Type", "application/json");
            await simpleResponse.WriteStringAsync(JsonSerializer.Serialize(new
            {
                Message = completion.Value.Content[0].Text
            }));
            return simpleResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Chat endpoint");
            
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync(JsonSerializer.Serialize(new
            {
                Success = false,
                Error = ex.Message
            }));
            
            return errorResponse;
        }
    }
}

// Request model
public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
}

