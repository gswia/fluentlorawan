namespace EelinkDecoder;

/// <summary>
/// Custom data for Battery Warning (0x03).
/// </summary>
public class BatteryWarningData
{
    /// <summary>External power voltage in mV.</summary>
    public ushort Power { get; set; }

    /// <summary>Internal battery voltage in mV.</summary>
    public ushort Battery { get; set; }

    public static BatteryWarningData Parse(byte[] data, ref int offset)
    {
        ushort power = (ushort)((data[offset] << 8) | data[offset + 1]);
        ushort battery = (ushort)((data[offset + 2] << 8) | data[offset + 3]);
        offset += 4;

        return new BatteryWarningData { Power = power, Battery = battery };
    }
}

/// <summary>
/// Custom data for Shock/Freefall/Tilt Warning (0x85, 0x86, 0x87).
/// </summary>
public class AccelerationWarningData
{
    /// <summary>X of the acceleration vector in g.</summary>
    public double X { get; set; }

    /// <summary>Y of the acceleration vector in g.</summary>
    public double Y { get; set; }

    /// <summary>Z of the acceleration vector in g.</summary>
    public double Z { get; set; }

    public static AccelerationWarningData Parse(byte[] data, ref int offset)
    {
        short x = (short)((data[offset] << 8) | data[offset + 1]);
        short y = (short)((data[offset + 2] << 8) | data[offset + 3]);
        short z = (short)((data[offset + 4] << 8) | data[offset + 5]);
        offset += 6;

        return new AccelerationWarningData
        {
            X = x / 256.0,
            Y = y / 256.0,
            Z = z / 256.0
        };
    }
}

/// <summary>
/// Custom data for Speed Warning (0x82).
/// </summary>
public class SpeedWarningData
{
    /// <summary>Current speed in km/h.</summary>
    public ushort Speed { get; set; }

    /// <summary>Low limit of speed in km/h.</summary>
    public ushort Low { get; set; }

    /// <summary>High limit of speed in km/h.</summary>
    public ushort High { get; set; }

    public static SpeedWarningData Parse(byte[] data, ref int offset)
    {
        ushort speed = (ushort)((data[offset] << 8) | data[offset + 1]);
        ushort low = (ushort)((data[offset + 2] << 8) | data[offset + 3]);
        ushort high = (ushort)((data[offset + 4] << 8) | data[offset + 5]);
        offset += 6;

        return new SpeedWarningData { Speed = speed, Low = low, High = high };
    }
}

/// <summary>
/// Custom data for Sensor Warning (0x20-0x24).
/// </summary>
public class SensorWarningData
{
    /// <summary>Current value (raw).</summary>
    public int Value { get; set; }

    /// <summary>Low limit (raw).</summary>
    public int Low { get; set; }

    /// <summary>High limit (raw).</summary>
    public int High { get; set; }

    /// <summary>Warning type to determine units.</summary>
    public byte WarningType { get; set; }

    public static SensorWarningData Parse(byte[] data, ref int offset, byte warningType)
    {
        int value = (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];
        int low = (data[offset + 4] << 24) | (data[offset + 5] << 16) | (data[offset + 6] << 8) | data[offset + 7];
        int high = (data[offset + 8] << 24) | (data[offset + 9] << 16) | (data[offset + 10] << 8) | data[offset + 11];
        offset += 12;

        return new SensorWarningData
        {
            Value = value,
            Low = low,
            High = high,
            WarningType = warningType
        };
    }

    /// <summary>Get human-readable value with units.</summary>
    public string GetFormattedValue()
    {
        return WarningType switch
        {
            0x20 => $"{Value / 256.0:F2}°C",        // Internal temperature
            0x21 => $"{Value / 10.0:F1}%",          // Humidity
            0x22 => $"{Value / 256.0:F2}lx",        // Illuminance
            0x23 => $"{Value}ppm",                  // CO2
            0x24 => $"{Value / 16.0:F2}°C",         // Probe temperature
            _ => Value.ToString()
        };
    }
}

/// <summary>
/// Warning Package (0x14) - Device → Server.
/// Sent when a specific warning occurs.
/// </summary>
public class Warning
{
    /// <summary>Package sequence number.</summary>
    public ushort Sequence { get; set; }

    /// <summary>Device position.</summary>
    public Position Location { get; set; } = new Position();

    /// <summary>Warning type.</summary>
    public byte WarningType { get; set; }

    /// <summary>Device status.</summary>
    public Status Status { get; set; } = new Status();

    /// <summary>Custom data for battery warning (0x03).</summary>
    public BatteryWarningData? BatteryData { get; set; }

    /// <summary>Custom data for shock/freefall/tilt warning (0x85, 0x86, 0x87).</summary>
    public AccelerationWarningData? AccelerationData { get; set; }

    /// <summary>Custom data for speed warning (0x82).</summary>
    public SpeedWarningData? SpeedData { get; set; }

    /// <summary>Custom data for sensor warnings (0x20-0x24).</summary>
    public SensorWarningData? SensorData { get; set; }

    /// <summary>Parse Warning packet from complete packet bytes (including 67 67 header).</summary>
    public static Warning Parse(byte[] data)
    {
        // Verify header
        if (data.Length < 5 || data[0] != 0x67 || data[1] != 0x67 || data[2] != 0x14)
            throw new ArgumentException("Invalid warning packet header");

        int size = (data[3] << 8) | data[4];
        if (data.Length < 5 + size)
            throw new ArgumentException("Packet size mismatch");

        int offset = 5;

        // Sequence: 2 bytes
        ushort sequence = (ushort)((data[offset] << 8) | data[offset + 1]);
        offset += 2;

        // Location: Position (variable length)
        var location = Position.Parse(data, ref offset);

        // Warning type: 1 byte
        byte warningType = data[offset++];

        // Status: 2 bytes
        var status = Status.Parse(data, offset);
        offset += 2;

        var warning = new Warning
        {
            Sequence = sequence,
            Location = location,
            WarningType = warningType,
            Status = status
        };

        // Parse custom data based on warning type
        if (offset < data.Length)
        {
            switch (warningType)
            {
                case 0x03: // Battery warning
                    warning.BatteryData = BatteryWarningData.Parse(data, ref offset);
                    break;

                case 0x85: // Shock
                case 0x86: // Freefall
                case 0x87: // Tilt
                    warning.AccelerationData = AccelerationWarningData.Parse(data, ref offset);
                    break;

                case 0x82: // Speed warning
                    warning.SpeedData = SpeedWarningData.Parse(data, ref offset);
                    break;

                case 0x20: // Temperature
                case 0x21: // Humidity
                case 0x22: // Illuminance
                case 0x23: // CO2
                case 0x24: // Probe temperature
                    warning.SensorData = SensorWarningData.Parse(data, ref offset, warningType);
                    break;

                // Other warnings (0x01, 0x02, 0x04, 0x05, 0x08, 0x09, 0x25, 0x26, 0x83, 0x84) have no custom data
            }
        }

        return warning;
    }

    /// <summary>Get warning type description.</summary>
    public string GetWarningDescription()
    {
        return WarningType switch
        {
            0x01 => "External power cut-off",
            0x02 => "SOS",
            0x03 => "Battery low",
            0x04 => "Activity warning",
            0x05 => "Shift warning",
            0x08 => "GPS antenna open-circuit",
            0x09 => "GPS antenna short-circuit",
            0x20 => "Out of internal temperature range",
            0x21 => "Out of humidity range",
            0x22 => "Out of illuminance range",
            0x23 => "Out of CO2 concentration range",
            0x24 => "Out of probe temperature range",
            0x25 => "Light wakeup warning",
            0x26 => "Motion wakeup warning",
            0x82 => "Under/over speed warning",
            0x83 => "In-to-fence warning",
            0x84 => "Out-of-fence warning",
            0x85 => "Shock warning",
            0x86 => "Free-fall warning",
            0x87 => "Tilt warning",
            _ => $"Unknown warning (0x{WarningType:X2})"
        };
    }

    public override string ToString()
    {
        return $"Warning: {GetWarningDescription()} at {Location.Time:yyyy-MM-dd HH:mm:ss} UTC";
    }
}
