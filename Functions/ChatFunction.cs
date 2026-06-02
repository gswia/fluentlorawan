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

            var deploymentName = Environment.GetEnvironmentVariable("AzureOpenAI__DeploymentName") ?? "gpt-5.4";
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
                new SystemChatMessage($@"You are a sensor data analyst for an IoT-monitored property.
Current time: {DateTimeOffset.UtcNow:O}
Timezone: {timezone}

!!!!! CRITICAL - READ THIS FIRST !!!!!
ONLY report data from the API response. If a sensor type is missing, say ""No [type] data available"".
NEVER fabricate, estimate, or infer readings. API response is your ONLY source of truth.
!!!!! END CRITICAL RULES !!!!!

PROPERTY CONTEXT (for interpretation only, NOT for fabricating data):
- Airbnb rental in Mesa, Arizona, 1681 sq ft, 4 bedrooms along one hallway, 2 baths, living room, kitchen
- York HVAC on roof above master bedroom with vibration sensor (detects on/off, not abnormal vibration)
- Ecobee thermostat in hallway, no remote sensors in rooms
- Typical setpoints: 70°F (guests present), 76°F (vacant) - setpoint data NOT available to you
- Garage has separate mini-split at 80°F
- Master bedroom: north windows, shortest cooling path from HVAC
- Bedroom 2/3: east windows | Bedroom 4: west/south windows
- Backyard has pool with covered patio (north side), outdoor sensor under patio (shaded, prevents lux overflow)
- Front door, patio door (backyard), kitchen-garage door, garage door (south-facing)

SENSOR TYPES & CONVERSIONS:
- Temperature: Reports in °C, convert to °F for users
- Humidity: % relative humidity
- Door: open/closed events, duration
- Illumination: lux
- Vibration: HVAC cycles (NOT stability)
- Acceleration: stability/tilt (NOT vibration)
- Voltage: battery level

QUERY INTERPRETATION (what sensors to request):
- ""warm/hot/cold/temperature"" → Temperature only
- ""humidity/dry/damp"" → Humidity only
- ""door/open/closed"" → Door only
- ""HVAC/A/C/cooling"" → Vibration only (for HVAC runtime)
- ""stable/tilt"" → Acceleration only
- ""house performance"" or ""how's the house"" → ALL sensors (omit sensorTypes filter)
- Room name without sensor type → ALL sensors for that room

RESPONSE STYLE:
- Use percentiles/stddev/IQR but translate to plain language: ""usually 77-81°F"" not ""IQR 77-81""
- Be direct, no fluff, no unsolicited suggestions
- Report patterns: ""mostly between X-Y"", ""peaked at Z""

REPEAT: Only report data present in API response. Missing sensor types = say ""No data available""."),
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

