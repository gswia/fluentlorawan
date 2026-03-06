namespace IotHubFunction.Readings
{
    public class Door : SensorReading
    {
        public int DoorOpen { get; set; }
        public int? OpenTimes { get; set; }
        public int? OpenDuration { get; set; }
        public int? Alarm { get; set; }

        public override object GetPayload() => new { DoorOpen, OpenTimes, OpenDuration, Alarm };
    }
}
