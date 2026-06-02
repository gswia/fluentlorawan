namespace NoReturn.LoRaWAN.Graph
{
    public class IntentSubjectPair
    {
        public string Intent { get; set; } = string.Empty;
        public List<string> Subjects { get; set; } = new();
    }
}
