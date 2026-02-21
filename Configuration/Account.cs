namespace IotHubFunction.Configuration
{
    public class Account
    {
        public string AccountId { get; set; } = string.Empty;
        public List<Application> Applications { get; set; } = new();
    }
}
