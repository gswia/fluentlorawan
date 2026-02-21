namespace IotHubFunction.Configuration
{
    public class Site
    {
        public string SiteId { get; set; } = string.Empty;
        public List<string> GatewayIds { get; set; } = new();
        public List<Device> Devices { get; set; } = new();
    }
}
