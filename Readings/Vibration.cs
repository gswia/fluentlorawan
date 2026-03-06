namespace IotHubFunction.Readings
{
    public class Vibration : SensorReading
    {
        public int? VibCount { get; set; }
        public int? WorkMin { get; set; }
        public int? Alarm { get; set; }
        public int? TDC { get; set; }

        public override object GetPayload() => new { VibCount, WorkMin, Alarm, TDC };
    }
}
