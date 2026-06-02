namespace NoReturn.LoRaWAN.Graph
{
    public class IntentSubjectPair
    {
        public string Intent { get; set; } = string.Empty;
        public List<string> SubjectNames { get; set; } = new();
        public List<string> SubjectTypes { get; set; } = new();
    }
}
