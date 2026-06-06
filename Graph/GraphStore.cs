namespace NoReturn.LoRaWAN.Graph
{
    public class GraphStore
    {
        public Dictionary<string, Subject> Subjects { get; set; } = new();
        public Dictionary<string, GraphSensor> Sensors { get; set; } = new();
        public List<SubjectRelationship> SubjectHierarchy { get; set; } = new();
        public List<SensorRelationship> SubjectSensors { get; set; } = new();

        // TODO: Remove this when we have real data loading
        public static GraphStore BuildSimpleTestGraph()
        {
            var graph = new GraphStore();

            // Create subjects
            var property = new Subject
            {
                Id = "property",
                Name = "Mesa Airbnb",
                Type = "property",
                Metadata = new Dictionary<string, string>
                {
                    { "address", "Mesa, AZ" }
                }
            };

            var masterBedroom = new Subject
            {
                Id = "master_bedroom",
                Name = "Master Bedroom",
                Type = "room.bedroom",
                Metadata = new Dictionary<string, string>
                {
                    { "floor", "1" },
                    { "windows", "2" }
                }
            };

            var bedroom4 = new Subject
            {
                Id = "bedroom_4",
                Name = "Bedroom 4",
                Type = "room.bedroom",
                Metadata = new Dictionary<string, string>
                {
                    { "floor", "2" },
                    { "windows", "1" }
                }
            };

            graph.Subjects.Add(property.Id, property);
            graph.Subjects.Add(masterBedroom.Id, masterBedroom);
            graph.Subjects.Add(bedroom4.Id, bedroom4);

            // Create sensors
            var masterTempSensor = new GraphSensor
            {
                Id = "sensor_master_temp",
                SensorId = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                DeviceId = "device_master",
                SensorType = SensorType.Temperature,
                Metadata = new Dictionary<string, string>
                {
                    { "unit", "fahrenheit" }
                }
            };

            var masterHumiditySensor = new GraphSensor
            {
                Id = "sensor_master_humidity",
                SensorId = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                DeviceId = "device_master",
                SensorType = SensorType.Humidity,
                Metadata = new Dictionary<string, string>
                {
                    { "unit", "percent" }
                }
            };

            var bed4TempSensor = new GraphSensor
            {
                Id = "sensor_bed4_temp",
                SensorId = Guid.Parse("10000000-0000-0000-0000-000000000003"),
                DeviceId = "device_bed4",
                SensorType = SensorType.Temperature,
                Metadata = new Dictionary<string, string>
                {
                    { "unit", "fahrenheit" }
                }
            };

            var bed4HumiditySensor = new GraphSensor
            {
                Id = "sensor_bed4_humidity",
                SensorId = Guid.Parse("10000000-0000-0000-0000-000000000004"),
                DeviceId = "device_bed4",
                SensorType = SensorType.Humidity,
                Metadata = new Dictionary<string, string>
                {
                    { "unit", "percent" }
                }
            };

            graph.Sensors.Add(masterTempSensor.Id, masterTempSensor);
            graph.Sensors.Add(masterHumiditySensor.Id, masterHumiditySensor);
            graph.Sensors.Add(bed4TempSensor.Id, bed4TempSensor);
            graph.Sensors.Add(bed4HumiditySensor.Id, bed4HumiditySensor);

            // Create subject hierarchy relationships
            graph.SubjectHierarchy.Add(new SubjectRelationship
            {
                ParentId = property.Id,
                ChildId = masterBedroom.Id
            });

            graph.SubjectHierarchy.Add(new SubjectRelationship
            {
                ParentId = property.Id,
                ChildId = bedroom4.Id
            });

            // Create sensor relationships
            graph.SubjectSensors.Add(new SensorRelationship
            {
                SubjectId = masterBedroom.Id,
                SensorId = masterTempSensor.Id
            });

            graph.SubjectSensors.Add(new SensorRelationship
            {
                SubjectId = masterBedroom.Id,
                SensorId = masterHumiditySensor.Id
            });

            graph.SubjectSensors.Add(new SensorRelationship
            {
                SubjectId = bedroom4.Id,
                SensorId = bed4TempSensor.Id
            });

            graph.SubjectSensors.Add(new SensorRelationship
            {
                SubjectId = bedroom4.Id,
                SensorId = bed4HumiditySensor.Id
            });

            return graph;
        }
    }
}
