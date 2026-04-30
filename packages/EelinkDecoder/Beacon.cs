namespace EelinkDecoder;

/// <summary>
/// Single BLE beacon data (16 bytes).
/// </summary>
public class Beacon
{
    /// <summary>BLE MAC address (6 bytes, reversed from wire format).</summary>
    public byte[] BleId { get; set; } = new byte[6];

    /// <summary>Signal level in dB.</summary>
    public sbyte RSSI { get; set; }

    /// <summary>Beacon model (e.g. 0xB1, 0xF2, 0x29).</summary>
    public byte Model { get; set; }

    /// <summary>Beacon version.</summary>
    public byte Version { get; set; }

    /// <summary>Battery voltage in mV.</summary>
    public ushort Battery { get; set; }

    /// <summary>Sensor temperature in °C.</summary>
    public double Temperature { get; set; }

    /// <summary>Self-defined data from beacon.</summary>
    public ushort SelfDefined { get; set; }

    /// <summary>Parse single Beacon from byte array at specified offset (big-endian).</summary>
    public static Beacon Parse(byte[] data, int offset)
    {
        // BLEID: 6 bytes reversed (wire format is reversed, so reverse it back)
        var bleId = new byte[6];
        for (int i = 0; i < 6; i++)
            bleId[5 - i] = data[offset + i];

        // RSSI: Signed 8-bit
        sbyte rssi = (sbyte)data[offset + 6];

        // Reserved at offset+7, skip

        // Model: 1 byte
        byte model = data[offset + 8];

        // Version: 1 byte
        byte version = data[offset + 9];

        // Battery: Unsigned 16-bit, in mV
        ushort battery = (ushort)((data[offset + 10] << 8) | data[offset + 11]);

        // Temperature: Signed 16-bit, in (1/256)°C
        short tempRaw = (short)((data[offset + 12] << 8) | data[offset + 13]);

        // Self-Defined: Unsigned 16-bit
        ushort selfDefined = (ushort)((data[offset + 14] << 8) | data[offset + 15]);

        return new Beacon
        {
            BleId = bleId,
            RSSI = rssi,
            Model = model,
            Version = version,
            Battery = battery,
            Temperature = tempRaw / 256.0,
            SelfDefined = selfDefined
        };
    }
}

/// <summary>
/// Beacons compound data type (2 + 16N bytes).
/// Contains array of BLE beacon data.
/// </summary>
public class Beacons
{
    /// <summary>Number of beacons.</summary>
    public byte Number { get; set; }

    /// <summary>Data ID (0x00: No BLE; 0x02: With Beacon).</summary>
    public byte DataId { get; set; }

    /// <summary>Array of beacon data.</summary>
    public Beacon[] BeaconList { get; set; } = Array.Empty<Beacon>();

    /// <summary>Parse Beacons from byte array at specified offset (big-endian).</summary>
    public static Beacons Parse(byte[] data, int offset)
    {
        byte number = data[offset];
        byte dataId = data[offset + 1];

        var beacons = new Beacon[number];
        int pos = offset + 2;

        for (int i = 0; i < number; i++)
        {
            beacons[i] = Beacon.Parse(data, pos);
            pos += 16;
        }

        return new Beacons
        {
            Number = number,
            DataId = dataId,
            BeaconList = beacons
        };
    }
}
