namespace IotHubFunction.Configuration
{
    public class Application
    {
        public string ApplicationId { get; set; } = string.Empty;
        public List<Site> Sites { get; set; } = new();
    }
}
