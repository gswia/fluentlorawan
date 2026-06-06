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

Matching rules:
- Match shortcuts naturally (""master"" → master_bedroom, ""bed 4"" → bedroom_4)
- Match plural categories (""bedrooms"" → all subjects with type room.bedroom)
- Match hierarchical queries (""rooms"" → all subjects with type starting with room.)
- If no specific location mentioned, match to 'property' subject (whole house)

Examples:
User: ""How warm is bedroom 4?""
Output: [{{""intent"": ""TEMPERATURE_CURRENT"", ""subjectIds"": [""bedroom_4""]}}]

User: ""How warm are the bedrooms?""
Output: [{{""intent"": ""TEMPERATURE_CURRENT"", ""subjectIds"": [""master_bedroom"", ""bedroom_4""]}}]

User: ""Temperature in master?""
Output: [{{""intent"": ""TEMPERATURE_CURRENT"", ""subjectIds"": [""master_bedroom""]}}]

User: ""Temperature in master bedroom and bedroom 4?""
Output: [{{""intent"": ""TEMPERATURE_CURRENT"", ""subjectIds"": [""master_bedroom"", ""bedroom_4""]}}]

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

            // Process each intent
            var results = new List<string>();

            foreach (var intentPair in intentPairs)
            {
                _logger.LogInformation("Processing intent: {Intent} with {Count} subject IDs", 
                    intentPair.Intent, intentPair.SubjectIds.Count);

                // Step 2: Lookup matched subjects from graph
                var matchedSubjects = intentPair.SubjectIds
                    .Select(id => graph.Subjects.TryGetValue(id, out var subject) ? subject : null)
                    .Where(s => s != null)
                    .Cast<Subject>()
                    .ToArray();

                if (matchedSubjects.Length == 0)
                {
                    results.Add($"[{intentPair.Intent}] No matching locations found.");
                    continue;
                }

                _logger.LogInformation("Matched {Count} subjects: {Subjects}", 
                    matchedSubjects.Length, string.Join(", ", matchedSubjects.Select(s => s.Name)));

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
                var output = await capability.ExecuteAsync(matchedSubjects, graph);
                results.Add(output.FinalPrompt);
            }

            var finalResult = string.Join("\n\n---\n\n", results);
            finalResult += $"\n\n{tokenInfo}";
            
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
