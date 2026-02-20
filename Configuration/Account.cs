namespace IotHubFunction.Configuration
{
    public class Account
    {
        public string AccountId { get; set; }
        public List<Application> Applications { get; set; } = new();
    }
}
