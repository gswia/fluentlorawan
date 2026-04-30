namespace EelinkDecoder;

/// <summary>
/// Hardware compound data type (20 bytes).
/// Contains battery, analog inputs, mileage, counters, and pedometer data.
/// </summary>
public class Hardware
{
    /// <summary>Device status.</summary>
    public Status Status { get; set; } = new Status();

    /// <summary>Battery voltage in mV.</summary>
    public ushort Battery { get; set; }

    /// <summary>AIN0 value in mV.</summary>
    public ushort AIN0 { get; set; }

    /// <summary>AIN1 value in mV.</summary>
    public ushort AIN1 { get; set; }

    /// <summary>Device mileage in meters (accumulated only when GPS is fixed).</summary>
    public uint Mileage { get; set; }

    /// <summary>GSM counter from last GSM command in minutes.</summary>
    public ushort GsmCounter { get; set; }

    /// <summary>GPS counter from last GPS command in minutes.</summary>
    public ushort GpsCounter { get; set; }

    /// <summary>Accumulated steps today.</summary>
    public ushort PdmStep { get; set; }

    /// <summary>Accumulated walking time today in seconds.</summary>
    public ushort PdmTime { get; set; }

    /// <summary>Parse Hardware from byte array at specified offset (big-endian).</summary>
    public static Hardware Parse(byte[] data, int offset)
    {
        // Status: 2 bytes
        var status = Status.Parse(data, offset);

        // Battery: Unsigned 16-bit, in mV
        ushort battery = (ushort)((data[offset + 2] << 8) | data[offset + 3]);

        // AIN0: Unsigned 16-bit, in mV
        ushort ain0 = (ushort)((data[offset + 4] << 8) | data[offset + 5]);

        // AIN1: Unsigned 16-bit, in mV
        ushort ain1 = (ushort)((data[offset + 6] << 8) | data[offset + 7]);

        // Mileage: Unsigned 32-bit, in meters
        uint mileage = (uint)((data[offset + 8] << 24) | (data[offset + 9] << 16) |
                              (data[offset + 10] << 8) | data[offset + 11]);

        // GSM Counter: Unsigned 16-bit, in minutes
        ushort gsmCounter = (ushort)((data[offset + 12] << 8) | data[offset + 13]);

        // GPS Counter: Unsigned 16-bit, in minutes
        ushort gpsCounter = (ushort)((data[offset + 14] << 8) | data[offset + 15]);

        // PDM Step: Unsigned 16-bit
        ushort pdmStep = (ushort)((data[offset + 16] << 8) | data[offset + 17]);

        // PDM Time: Unsigned 16-bit, in seconds
        ushort pdmTime = (ushort)((data[offset + 18] << 8) | data[offset + 19]);

        return new Hardware
        {
            Status = status,
            Battery = battery,
            AIN0 = ain0,
            AIN1 = ain1,
            Mileage = mileage,
            GsmCounter = gsmCounter,
            GpsCounter = gpsCounter,
            PdmStep = pdmStep,
            PdmTime = pdmTime
        };
    }
}
