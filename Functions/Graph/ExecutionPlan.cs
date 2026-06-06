namespace NoReturn.LoRaWAN.Graph;

public class ExecutionPlan
{
    public List<ExecutionStep> Steps { get; set; } = new();
    public TokenUsage Tokens { get; set; } = new();
}

public class ExecutionStep
{
    public string Intent { get; set; } = string.Empty;
    public List<string> SubjectIds { get; set; } = new();
    public List<string> Capabilities { get; set; } = new();
    public Guid AccountId { get; set; }
    public List<Guid> SensorIds { get; set; } = new();
    public string Timezone { get; set; } = string.Empty;
    public string TimeWindow { get; set; } = string.Empty;
    public string? CompareWith { get; set; }
}

public class TokenUsage
{
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public int TotalTokens { get; set; }
}
