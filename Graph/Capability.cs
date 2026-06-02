namespace NoReturn.LoRaWAN.Graph
{
    public abstract class Capability
    {
        public abstract bool AppliesTo(Subject[] subjects);
        public abstract Task<CapabilityOutput> ExecuteAsync(Subject[] subjects, GraphStore graph);
    }
}
