namespace EelinkDecoder;

/// <summary>
/// Sensors compound data type (14 bytes).
/// Contains temperature, humidity, illuminance, CO2, and probe temperature.
/// </summary>
public class Sensors
{
    /// <summary>Internal temperature in °C.</summary>
    public double Temperature { get; set; }

    /// <summary>Humidity in %.</summary>
    public double Humidity { get; set; }

    /// <summary>Illuminance in lx.</summary>
    public double Illuminance { get; set; }

    /// <summary>CO2 concentration in ppm.</summary>
    public uint CO2 { get; set; }

    /// <summary>Probe temperature in °C.</summary>
    public double ProbeTemperature { get; set; }

    /// <summary>Parse Sensors from byte array at specified offset (big-endian).</summary>
    public static Sensors Parse(byte[] data, int offset)
    {
        // Temperature: Signed 16-bit, in (1/256)°C
        short tempRaw = (short)((data[offset] << 8) | data[offset + 1]);
        
        // Humidity: Unsigned 16-bit, in (1/10)%
        ushort humidityRaw = (ushort)((data[offset + 2] << 8) | data[offset + 3]);
        
        // Illuminance: Unsigned 32-bit, in (1/256)lx
        uint illuminanceRaw = (uint)((data[offset + 4] << 24) | (data[offset + 5] << 16) | 
                                     (data[offset + 6] << 8) | data[offset + 7]);
        
        // CO2: Unsigned 32-bit, in ppm
        uint co2 = (uint)((data[offset + 8] << 24) | (data[offset + 9] << 16) | 
                          (data[offset + 10] << 8) | data[offset + 11]);
        
        // Probe: Signed 16-bit, in (1/16)°C
        short probeRaw = (short)((data[offset + 12] << 8) | data[offset + 13]);

        return new Sensors
        {
            Temperature = tempRaw / 256.0,
            Humidity = humidityRaw / 10.0,
            Illuminance = illuminanceRaw / 256.0,
            CO2 = co2,
            ProbeTemperature = probeRaw / 16.0
        };
    }
}
