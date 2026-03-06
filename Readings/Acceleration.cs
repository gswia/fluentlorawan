namespace IotHubFunction.Readings
{
    public class Acceleration : SensorReading
    {
        public double? MaxAccX { get; set; }
        public double? MaxAccY { get; set; }
        public double? MaxAccZ { get; set; }

        public override object GetPayload() => new { MaxAccX, MaxAccY, MaxAccZ };
    }
}
