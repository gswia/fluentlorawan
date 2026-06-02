namespace NoReturn.LoRaWAN.Graph
{
    public class Subject
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "room.bedroom", "equipment.hvac", etc
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}
