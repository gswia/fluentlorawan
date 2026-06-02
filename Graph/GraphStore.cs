namespace NoReturn.LoRaWAN.Graph
{
    public class GraphStore
    {
        public Dictionary<string, Subject> Subjects { get; set; } = new();
        public Dictionary<string, GraphSensor> Sensors { get; set; } = new();
        public List<SubjectRelationship> SubjectHierarchy { get; set; } = new();
        public List<SensorRelationship> SubjectSensors { get; set; } = new();
    }
}
