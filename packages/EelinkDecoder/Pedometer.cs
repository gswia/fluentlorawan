namespace EelinkDecoder;

/// <summary>
/// Pedometer Package (0x1A) - Device → Server.
/// If device has an accelerometer, it analyzes acceleration data as a pedometer.
/// The result is sent to server per day.
/// </summary>
public class Pedometer
{
    /// <summary>Package sequence number.</summary>
    public ushort Sequence { get; set; }

    /// <summary>UTC time of last day (Unix timestamp).</summary>
    public uint Today { get; set; }

    /// <summary>UTC time of last day as DateTime.</summary>
    public DateTime TodayDateTime => DateTimeOffset.FromUnixTimeSeconds(Today).UtcDateTime;

    /// <summary>Accumulated steps in total.</summary>
    public uint AllStep { get; set; }

    /// <summary>Accumulated walking time in total (in seconds).</summary>
    public uint AllTime { get; set; }

    /// <summary>Accumulated walking distance in total (in mm).</summary>
    public uint AllDistance { get; set; }

    /// <summary>Accumulated burnt energy in total (in cal).</summary>
    public uint AllEnergy { get; set; }

    /// <summary>Accumulated steps in last day.</summary>
    public uint DayStep { get; set; }

    /// <summary>Accumulated walking time in last day (in seconds).</summary>
    public uint DayTime { get; set; }

    /// <summary>Accumulated walking distance in last day (in mm).</summary>
    public uint DayDistance { get; set; }

    /// <summary>Accumulated burnt energy in last day (in cal).</summary>
    public uint DayEnergy { get; set; }

    /// <summary>Parse Pedometer packet from complete packet bytes (including 67 67 header).</summary>
    public static Pedometer Parse(byte[] data)
    {
        // Verify header
        if (data.Length < 5 || data[0] != 0x67 || data[1] != 0x67 || data[2] != 0x1A)
            throw new ArgumentException("Invalid pedometer packet header");

        int size = (data[3] << 8) | data[4];
        if (data.Length < 5 + size)
            throw new ArgumentException("Packet size mismatch");

        int offset = 5;

        // Sequence: 2 bytes
        ushort sequence = (ushort)((data[offset] << 8) | data[offset + 1]);
        offset += 2;

        // Today: 4 bytes (Unix timestamp)
        uint today = (uint)((data[offset] << 24) | (data[offset + 1] << 16) |
                            (data[offset + 2] << 8) | data[offset + 3]);
        offset += 4;

        // All Step: 4 bytes
        uint allStep = (uint)((data[offset] << 24) | (data[offset + 1] << 16) |
                              (data[offset + 2] << 8) | data[offset + 3]);
        offset += 4;

        // All Time: 4 bytes
        uint allTime = (uint)((data[offset] << 24) | (data[offset + 1] << 16) |
                              (data[offset + 2] << 8) | data[offset + 3]);
        offset += 4;

        // All Distance: 4 bytes
        uint allDistance = (uint)((data[offset] << 24) | (data[offset + 1] << 16) |
                                  (data[offset + 2] << 8) | data[offset + 3]);
        offset += 4;

        // All Energy: 4 bytes
        uint allEnergy = (uint)((data[offset] << 24) | (data[offset + 1] << 16) |
                                (data[offset + 2] << 8) | data[offset + 3]);
        offset += 4;

        // Day Step: 4 bytes
        uint dayStep = (uint)((data[offset] << 24) | (data[offset + 1] << 16) |
                              (data[offset + 2] << 8) | data[offset + 3]);
        offset += 4;

        // Day Time: 4 bytes
        uint dayTime = (uint)((data[offset] << 24) | (data[offset + 1] << 16) |
                              (data[offset + 2] << 8) | data[offset + 3]);
        offset += 4;

        // Day Distance: 4 bytes
        uint dayDistance = (uint)((data[offset] << 24) | (data[offset + 1] << 16) |
                                  (data[offset + 2] << 8) | data[offset + 3]);
        offset += 4;

        // Day Energy: 4 bytes
        uint dayEnergy = (uint)((data[offset] << 24) | (data[offset + 1] << 16) |
                                (data[offset + 2] << 8) | data[offset + 3]);

        return new Pedometer
        {
            Sequence = sequence,
            Today = today,
            AllStep = allStep,
            AllTime = allTime,
            AllDistance = allDistance,
            AllEnergy = allEnergy,
            DayStep = dayStep,
            DayTime = dayTime,
            DayDistance = dayDistance,
            DayEnergy = dayEnergy
        };
    }

    public override string ToString()
    {
        return $"Pedometer #{Sequence} for {TodayDateTime:yyyy-MM-dd} - " +
               $"Day: {DayStep} steps, {DayDistance / 1000.0:F2}m, {DayEnergy}cal";
    }
}
