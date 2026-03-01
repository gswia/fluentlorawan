namespace IotHubFunction.Readings
{
    public abstract class GatewayReading : Reading
    {
        public string GatewayId { get; set; } = string.Empty;
    }
}
