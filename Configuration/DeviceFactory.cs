namespace IotHubFunction.Configuration
{
    public static class DeviceFactory
    {
        private static readonly Dictionary<string, Func<Device>> _deviceTypes = new()
        {
            { "LHT52", () => new LHT52Device() },
            { "LHT65N", () => new LHT65NDevice() }
        };

        public static Device Create(string deviceProfileName)
        {
            if (_deviceTypes.TryGetValue(deviceProfileName, out var factory))
                return factory();

            throw new NotSupportedException($"Device type '{deviceProfileName}' is not supported");
        }
    }
}
