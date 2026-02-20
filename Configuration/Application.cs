namespace IotHubFunction.Configuration
{
    public class Application
    {
        public string ApplicationId { get; set; }
        public List<Device> Devices { get; set; } = new();
    }
}
