namespace EelinkDecoder;

/// <summary>
/// Location Package (0x12) - Device → Server.
/// The most important package. Transfers position and other information.
/// </summary>
public class Location
{
    /// <summary>Package sequence number.</summary>
    public ushort Sequence { get; set; }

    /// <summary>Device position (GPS, cell towers, WiFi).</summary>
    public Position Position { get; set; } = new Position();

    /// <summary>Hardware data (battery, mileage, etc).</summary>
    public Hardware Hardware { get; set; } = new Hardware();

    /// <summary>Sensors data (temperature, humidity, etc).</summary>
    public Sensors Sensors { get; set; } = new Sensors();

    /// <summary>Beacon data (BLE beacons).</summary>
    public Beacons Beacons { get; set; } = new Beacons();

    /// <summary>Parse Location packet from complete packet bytes (including 67 67 header).</summary>
    public static Location Parse(byte[] data)
    {
        // Verify header
        if (data.Length < 5 || data[0] != 0x67 || data[1] != 0x67 || data[2] != 0x12)
            throw new ArgumentException("Invalid location packet header");

        int size = (data[3] << 8) | data[4];
        if (data.Length < 5 + size)
            throw new ArgumentException("Packet size mismatch");

        int offset = 5;

        // Sequence: 2 bytes
        ushort sequence = (ushort)((data[offset] << 8) | data[offset + 1]);
        offset += 2;

        // Location: Position (variable length)
        var position = Position.Parse(data, ref offset);

        // Hardware: 20 bytes
        var hardware = Hardware.Parse(data, offset);
        offset += 20;

        // Sensors: 14 bytes
        var sensors = Sensors.Parse(data, offset);
        offset += 14;

        // Beacons: 2+16N bytes (variable length based on beacon count)
        var beacons = Beacons.Parse(data, offset);

        return new Location
        {
            Sequence = sequence,
            Position = position,
            Hardware = hardware,
            Sensors = sensors,
            Beacons = beacons
        };
    }

    public override string ToString()
    {
        var gps = Position.Gps != null 
            ? $"GPS: {Position.Gps.Latitude:F6}, {Position.Gps.Longitude:F6}, {Position.Gps.Speed}km/h"
            : "No GPS";
        
        return $"Location #{Sequence} at {Position.Time:yyyy-MM-dd HH:mm:ss} UTC - {gps}, Battery: {Hardware.Battery}mV";
    }
}
