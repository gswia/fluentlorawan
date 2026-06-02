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

Return a JSON array of objects with:
- 'intent': the intent name
- 'subjectNames': array of specific location names (""bedroom 4"", ""master bedroom"")
- 'subjectTypes': array of category types (""bedrooms"", ""rooms"", ""equipment"")

IMPORTANT: Expand common shortcuts to full names:
- ""master"" → ""master bedroom""
- ""primary"" → ""primary bedroom""
- ""bed 4"" → ""bedroom 4""
- ""br 4"" → ""bedroom 4""

Examples:
User: ""How warm is bedroom 4?""
Output: [{""intent"": ""TEMPERATURE_CURRENT"", ""subjectNames"": [""bedroom 4""], ""subjectTypes"": []}]

User: ""How warm are the bedrooms?""
Output: [{""intent"": ""TEMPERATURE_CURRENT"", ""subjectNames"": [], ""subjectTypes"": [""bedrooms""]}]

User: ""Temperature in master?""
Output: [{""intent"": ""TEMPERATURE_CURRENT"", ""subjectNames"": [""master bedroom""], ""subjectTypes"": []}]

User: ""Temperature in master bedroom and bedroom 4?""
Output: [{""intent"": ""TEMPERATURE_CURRENT"", ""subjectNames"": [""master bedroom"", ""bedroom 4""], ""subjectTypes"": []}]

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

            // Pre-compute embeddings for graph subjects
            var embeddingClient = _openAiClient.GetEmbeddingClient("text-embedding-3-large");
            var subjectNames = graph.Subjects.Values.Select(s => s.Name).ToList();
            var subjectEmbeddings = await embeddingClient.GenerateEmbeddingsAsync(subjectNames);
            var subjectEmbeddingMap = new Dictionary<string, float[]>();
            for (int i = 0; i < subjectNames.Count; i++)
            {
                subjectEmbeddingMap[subjectNames[i]] = subjectEmbeddings.Value[i].ToFloats().ToArray();
            }

            // Pre-compute type embeddings
            var typeStrings = new List<string> { "bedroom sleeping room", "room", "bathroom", "equipment" };
            var typeEmbeddings = await embeddingClient.GenerateEmbeddingsAsync(typeStrings);
            var typeEmbeddingMap = new Dictionary<string, float[]>
            {
                ["room.bedroom"] = typeEmbeddings.Value[0].ToFloats().ToArray(),
                ["room"] = typeEmbeddings.Value[1].ToFloats().ToArray(),
                ["room.bathroom"] = typeEmbeddings.Value[2].ToFloats().ToArray(),
                ["equipment"] = typeEmbeddings.Value[3].ToFloats().ToArray()
            };

            // Process each intent
            var results = new List<string>();

            foreach (var intentPair in intentSubjects)
            {
                _logger.LogInformation("Processing intent: {Intent} with {NameCount} names, {TypeCount} types", 
                    intentPair.Intent, intentPair.SubjectNames.Count, intentPair.SubjectTypes.Count);

                // Step 2: Match subjects using embeddings
                var matchedSubjects = new List<Subject>();

                // Match by type (structural filter)
                foreach (var typeQuery in intentPair.SubjectTypes)
                {
                    var typeEmbedding = await embeddingClient.GenerateEmbeddingAsync(typeQuery);
                    var typeVector = typeEmbedding.Value.ToFloats().ToArray();

                    // Find matching type via embedding similarity
                    var typeMatches = graph.Subjects.Values
                        .Where(s => typeEmbeddingMap.ContainsKey(s.Type))
                        .Select(s => (Subject: s, Similarity: CosineSimilarity(typeVector, typeEmbeddingMap[s.Type])))
                        .Where(x => x.Similarity > 0.7f)
                        .Select(x => x.Subject);

                    matchedSubjects.AddRange(typeMatches);
                    _logger.LogInformation("Matched type '{Type}' to {Count} subjects", typeQuery, typeMatches.Count());
                }

                // Match by name (semantic search)
                foreach (var subjectName in intentPair.SubjectNames)
                {
                    var nameEmbedding = await embeddingClient.GenerateEmbeddingAsync(subjectName);
                    var nameVector = nameEmbedding.Value.ToFloats().ToArray();

                    var nameMatches = graph.Subjects.Values
                        .Where(s => subjectEmbeddingMap.ContainsKey(s.Name))
                        .Select(s => (Subject: s, Similarity: CosineSimilarity(nameVector, subjectEmbeddingMap[s.Name])))
                        .Where(x => x.Similarity > 0.7f)
                        .OrderByDescending(x => x.Similarity)
                        .Take(3)
                        .Select(x => x.Subject);

                    matchedSubjects.AddRange(nameMatches);
                    _logger.LogInformation("Matched name '{Name}' to {Count} subjects", subjectName, nameMatches.Count());
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

    private static float CosineSimilarity(float[] a, float[] b)
    {
        var dot = 0f;
        var magA = 0f;
        var magB = 0f;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }
        return dot / (MathF.Sqrt(magA) * MathF.Sqrt(magB));
    }
}
