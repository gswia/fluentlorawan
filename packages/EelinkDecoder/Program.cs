namespace EelinkDecoder;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: EelinkDecoder <hex_payload>");
            return;
        }

        var hex = args[0];
        var data = Convert.FromHexString(hex);

        Console.WriteLine($"Raw: {Convert.ToHexString(data)}");
        Console.WriteLine($"Length: {data.Length} bytes");
        
        if (data.Length < 3 || data[0] != 0x67 || data[1] != 0x67)
        {
            Console.WriteLine("Invalid packet header");
            return;
        }

        var packetType = data[2];
        Console.WriteLine($"Packet Type: 0x{packetType:X2}");
        Console.WriteLine();

        switch (packetType)
        {
            case 0x01: // Login
                DecodeLogin(data);
                break;

            case 0x03: // Heartbeat
                DecodeHeartbeat(data);
                break;

            case 0x12: // Location
                DecodeLocation(data);
                break;

            case 0x14: // Warning
                DecodeWarning(data);
                break;

            case 0x1A: // Pedometer
                DecodePedometer(data);
                break;

            default:
                Console.WriteLine($"Unsupported packet type: 0x{packetType:X2}");
                break;
        }
    }

    static void DecodeLogin(byte[] data)
    {
        var login = Login.Parse(data);

        Console.WriteLine(login);
        Console.WriteLine();
        Console.WriteLine($"Sequence: {login.Sequence}");
        Console.WriteLine($"IMEI: {login.IMEI}");
        Console.WriteLine($"Language: {(login.Language == 0 ? "Chinese" : "English")}");
        Console.WriteLine($"Timezone: {login.Timezone * 15} minutes");
        Console.WriteLine();
        Console.WriteLine($"System Version: {Login.FormatVersion(login.SystemVersion)}");
        Console.WriteLine($"App Version: {Login.FormatVersion(login.AppVersion)}");
        Console.WriteLine();
        Console.WriteLine($"Param-set Version: {login.ParamSetVersion}");
        Console.WriteLine($"Param-set Original Size: {login.ParamSetOriginalSize} bytes");
        Console.WriteLine($"Param-set Compressed Size: {login.ParamSetCompressedSize} bytes");
        Console.WriteLine($"Param-set Checksum: 0x{login.ParamSetChecksum:X4}");
        Console.WriteLine();
    }

    static void DecodeHeartbeat(byte[] data)
    {
        var heartbeat = Heartbeat.Parse(data);

        Console.WriteLine(heartbeat);
        Console.WriteLine();
        Console.WriteLine($"Sequence: {heartbeat.Sequence}");
        Console.WriteLine();

        PrintStatus(heartbeat.Status);
    }

    static void DecodeWarning(byte[] data)
    {
        var warning = Warning.Parse(data);

        Console.WriteLine(warning);
        Console.WriteLine();
        Console.WriteLine($"Sequence: {warning.Sequence}");
        Console.WriteLine($"Time: {warning.Location.Time:yyyy-MM-dd HH:mm:ss} UTC");
        Console.WriteLine($"Warning Type: 0x{warning.WarningType:X2} - {warning.GetWarningDescription()}");
        Console.WriteLine();

        PrintGps(warning.Location.Gps);
        PrintStatus(warning.Status);

        if (warning.SpeedData != null)
        {
            Console.WriteLine("Speed Warning Data:");
            Console.WriteLine($"  Current: {warning.SpeedData.Speed} km/h");
            Console.WriteLine($"  Low Limit: {warning.SpeedData.Low} km/h");
            Console.WriteLine($"  High Limit: {warning.SpeedData.High} km/h");
            Console.WriteLine();
        }

        if (warning.BatteryData != null)
        {
            Console.WriteLine("Battery Warning Data:");
            Console.WriteLine($"  Power: {warning.BatteryData.Power} mV");
            Console.WriteLine($"  Battery: {warning.BatteryData.Battery} mV");
            Console.WriteLine();
        }

        if (warning.AccelerationData != null)
        {
            Console.WriteLine("Acceleration Data:");
            Console.WriteLine($"  X: {warning.AccelerationData.X:F3}g");
            Console.WriteLine($"  Y: {warning.AccelerationData.Y:F3}g");
            Console.WriteLine($"  Z: {warning.AccelerationData.Z:F3}g");
            Console.WriteLine();
        }

        if (warning.SensorData != null)
        {
            Console.WriteLine("Sensor Warning Data:");
            Console.WriteLine($"  Current: {warning.SensorData.GetFormattedValue()}");
            Console.WriteLine($"  Low Limit: {warning.SensorData.Low}");
            Console.WriteLine($"  High Limit: {warning.SensorData.High}");
            Console.WriteLine();
        }
    }

    static void DecodePedometer(byte[] data)
    {
        var pedometer = Pedometer.Parse(data);

        Console.WriteLine(pedometer);
        Console.WriteLine();
        Console.WriteLine($"Sequence: {pedometer.Sequence}");
        Console.WriteLine($"Date: {pedometer.TodayDateTime:yyyy-MM-dd}");
        Console.WriteLine();

        Console.WriteLine("Totals (All Time):");
        Console.WriteLine($"  Steps: {pedometer.AllStep:N0}");
        Console.WriteLine($"  Time: {FormatDuration(pedometer.AllTime)}");
        Console.WriteLine($"  Distance: {pedometer.AllDistance / 1000.0:F2} m ({pedometer.AllDistance / 1000000.0:F2} km)");
        Console.WriteLine($"  Energy: {pedometer.AllEnergy:N0} cal");
        Console.WriteLine();

        Console.WriteLine("Last Day:");
        Console.WriteLine($"  Steps: {pedometer.DayStep:N0}");
        Console.WriteLine($"  Time: {FormatDuration(pedometer.DayTime)}");
        Console.WriteLine($"  Distance: {pedometer.DayDistance / 1000.0:F2} m ({pedometer.DayDistance / 1000000.0:F2} km)");
        Console.WriteLine($"  Energy: {pedometer.DayEnergy:N0} cal");
        Console.WriteLine();
    }

    static void DecodeLocation(byte[] data)
    {
        var location = Location.Parse(data);

        Console.WriteLine(location);
        Console.WriteLine();
        Console.WriteLine($"Sequence: {location.Sequence}");
        Console.WriteLine($"Time: {location.Position.Time:yyyy-MM-dd HH:mm:ss} UTC");
        Console.WriteLine();

        PrintGps(location.Position.Gps);
        PrintStatus(location.Hardware.Status);

        Console.WriteLine("Hardware:");
        Console.WriteLine($"  Battery: {location.Hardware.Battery} mV");
        Console.WriteLine($"  Mileage: {location.Hardware.Mileage} m");
        Console.WriteLine($"  GPS Counter: {location.Hardware.GpsCounter} min");
        Console.WriteLine($"  GSM Counter: {location.Hardware.GsmCounter} min");
        Console.WriteLine();

        Console.WriteLine("Sensors:");
        Console.WriteLine($"  Temperature: {location.Sensors.Temperature:F2}°C");
        Console.WriteLine($"  Humidity: {location.Sensors.Humidity:F1}%");
        Console.WriteLine($"  Illuminance: {location.Sensors.Illuminance:F2}lx");
        Console.WriteLine($"  CO2: {location.Sensors.CO2} ppm");
        Console.WriteLine($"  Probe Temp: {location.Sensors.ProbeTemperature:F2}°C");
        Console.WriteLine();

        if (location.Beacons.Number > 0)
        {
            Console.WriteLine($"Beacons: {location.Beacons.Number}");
            foreach (var beacon in location.Beacons.BeaconList)
            {
                var macStr = string.Join(":", beacon.BleId.Select(b => b.ToString("X2")));
                Console.WriteLine($"  {macStr} RSSI={beacon.RSSI}dB Model=0x{beacon.Model:X2} Battery={beacon.Battery}mV Temp={beacon.Temperature:F2}°C");
            }
            Console.WriteLine();
        }
    }

    static void PrintGps(GpsData? gps)
    {
        if (gps != null)
        {
            Console.WriteLine("GPS:");
            Console.WriteLine($"  Lat: {gps.Latitude:F6}°");
            Console.WriteLine($"  Lng: {gps.Longitude:F6}°");
            Console.WriteLine($"  Speed: {gps.Speed} km/h");
            Console.WriteLine($"  Course: {gps.Course}°");
            Console.WriteLine($"  Altitude: {gps.Altitude}m");
            Console.WriteLine($"  Satellites: {gps.Satellites}");
            Console.WriteLine();
        }
    }

    static void PrintStatus(Status status)
    {
        Console.WriteLine("Status:");
        Console.WriteLine($"  GPS Fixed: {status.GpsFixed}");
        Console.WriteLine($"  Car Device: {status.IsCarDevice}");
        Console.WriteLine($"  Engine On: {status.EngineOn}");
        Console.WriteLine($"  Active: {status.IsActive}");
        Console.WriteLine($"  GPS Running: {status.GpsRunning}");
        Console.WriteLine();
    }

    static string FormatDuration(uint seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}h {ts.Minutes}m {ts.Seconds}s ({seconds:N0} sec)";
        else if (ts.TotalMinutes >= 1)
            return $"{ts.Minutes}m {ts.Seconds}s ({seconds:N0} sec)";
        else
            return $"{seconds:N0} sec";
    }
}
