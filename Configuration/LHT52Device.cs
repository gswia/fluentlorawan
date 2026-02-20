using IotHubFunction.Sensors;

namespace IotHubFunction.Configuration
{
    public class LHT52Device : Device
    {
        public TemperatureSensor TemperatureSensor { get; set; }
        public HumiditySensor HumiditySensor { get; set; }
        public TemperatureProbeSensor TemperatureProbeSensor { get; set; }
    }
}
