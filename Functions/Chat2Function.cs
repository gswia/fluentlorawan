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

            // Step 1: Extract intents and subjects using LLM
            var deploymentName = Environment.GetEnvironmentVariable("AzureOpenAI__DeploymentName") ?? "gpt-5.4";
            var chatClient = _openAiClient.GetChatClient(deploymentName);

            var extractionPrompt = @"You are an intent and location extractor for an IoT sensor monitoring system.

Extract the user's intent(s) and the locations/subjects they're asking about from their message.

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

Return a JSON array of objects with 'intent' and 'subjects' (array of location strings).
If no specific location is mentioned, use an empty array for subjects.
If asking about the whole house, use HOUSE_OVERVIEW intent with empty subjects array.

Examples:
User: ""How warm is bedroom 4?""
Output: [{""intent"": ""TEMPERATURE_CURRENT"", ""subjects"": [""bedroom 4""]}]

User: ""Are any doors open?""
Output: [{""intent"": ""DOOR_STATUS"", ""subjects"": []}]

User: ""Temperature in living room and bedroom 2?""
Output: [{""intent"": ""TEMPERATURE_CURRENT"", ""subjects"": [""living room"", ""bedroom 2""]}]

Now extract from this user message:";

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(extractionPrompt),
                new UserChatMessage(chatRequest.Message)
            };

            var completion = await chatClient.CompleteChatAsync(messages);
            var extractedJson = completion.Value.Content[0].Text;

            _logger.LogInformation("Extracted intents: {Json}", extractedJson);

            // Parse the extracted intents
            var intentSubjects = JsonSerializer.Deserialize<List<IntentSubjectPair>>(extractedJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (intentSubjects == null || intentSubjects.Count == 0)
            {
                var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequest.WriteStringAsync("Could not extract intent from message");
                return badRequest;
            }

            // Build simple test graph
            var graph = GraphStore.BuildSimpleTestGraph();
            _logger.LogInformation("Graph loaded: {SubjectCount} subjects, {SensorCount} sensors", 
                graph.Subjects.Count, graph.Sensors.Count);

            // Process each intent
            var results = new List<string>();
            foreach (var intentPair in intentSubjects)
            {
                _logger.LogInformation("Processing intent: {Intent} with {SubjectCount} subjects", 
                    intentPair.Intent, intentPair.Subjects.Count);

                // Step 2: Match subjects to graph (simple string matching for now, embeddings later)
                var matchedSubjects = new List<Subject>();
                foreach (var subjectName in intentPair.Subjects)
                {
                    // Simple case-insensitive name matching
                    var match = graph.Subjects.Values
                        .FirstOrDefault(s => s.Name.Equals(subjectName, StringComparison.OrdinalIgnoreCase));
                    
                    if (match != null)
                    {
                        matchedSubjects.Add(match);
                        _logger.LogInformation("Matched subject '{SubjectName}' to {SubjectId}", 
                            subjectName, match.Id);
                    }
                    else
                    {
                        _logger.LogWarning("Could not match subject: {SubjectName}", subjectName);
                    }
                }

                if (matchedSubjects.Count == 0)
                {
                    results.Add($"[{intentPair.Intent}] No matching locations found");
                    continue;
                }

                // Step 3: Select capability based on intent
                Capability? capability = intentPair.Intent switch
                {
                    "TEMPERATURE_CURRENT" => new TemperatureCapability(),
                    "TEMPERATURE_COMPARISON" => new TemperatureCapability(),
                    // TODO: Add other capabilities (Humidity, Door, etc.)
                    _ => null
                };

                if (capability == null)
                {
                    results.Add($"[{intentPair.Intent}] No capability handler implemented yet");
                    continue;
                }

                // Step 4: Execute capability
                var output = await capability.ExecuteAsync(matchedSubjects.ToArray(), graph);
                results.Add(output.FinalPrompt);
            }

            var finalResult = string.Join("\n\n---\n\n", results);
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteStringAsync(finalResult);
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
