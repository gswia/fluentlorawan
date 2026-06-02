namespace NoReturn.LoRaWAN.Graph
{
    public class TemperatureCapability : Capability
    {
        public override bool AppliesTo(Subject[] subjects)
        {
            // Applies to any subject with type starting with "room"
            return subjects.Any(s => s.Type.StartsWith("room"));
        }

        public override async Task<CapabilityOutput> ExecuteAsync(Subject[] subjects, GraphStore graph)
        {
            var sensorIds = new List<string>();
            var contexts = new List<string>();

            foreach (var subject in subjects)
            {
                // Find temperature sensors for this subject
                var sensorRels = graph.SubjectSensors
                    .Where(sr => sr.SubjectId == subject.Id)
                    .ToList();

                foreach (var rel in sensorRels)
                {
                    if (graph.Sensors.TryGetValue(rel.SensorId, out var sensor))
                    {
                        if (sensor.SensorType == SensorType.Temperature)
                        {
                            sensorIds.Add(sensor.Id);
                        }
                    }
                }

                // Build context from metadata
                var contextParts = new List<string>();
                
                if (subject.Metadata.TryGetValue("windows", out var windows))
                {
                    contextParts.Add(windows);
                }

                // TODO: Walk DUCT_CONTINUES relationships to calculate position
                // For now, just use subject name
                if (contextParts.Count > 0)
                {
                    contexts.Add($"{subject.Name}: {string.Join(", ", contextParts)}");
                }
            }

            // Build context string
            var contextString = string.Join("\n", contexts);

            // TODO: Call analytics API here with sensorIds
            // var toolResult = await CallAnalyticsApiAsync(sensorIds, "1 hours");
            var toolResult = "[TemperatureCapability will call analytics API here]";

            // Combine context + tool results
            var finalPrompt = $@"Context:
{contextString}

Sensor Data:
{toolResult}";

            return new CapabilityOutput
            {
                FinalPrompt = finalPrompt
            };
        }
    }
}
