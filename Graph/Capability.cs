namespace NoReturn.LoRaWAN.Graph
{
    public abstract class Capability
    {
        public abstract Task<CapabilityOutput> ExecuteAsync(Subject[] subjects, GraphStore graph);
    }
}
