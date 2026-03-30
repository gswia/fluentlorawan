namespace EelinkDecoder;

/// <summary>
/// Heartbeat packet (0x03) - TCP keep-alive
/// </summary>
public class Heartbeat
{
    public ushort Sequence { get; set; }
    public Status Status { get; set; }

    public static Heartbeat Parse(byte[] data)
    {
        if (data.Length < 5 || data[0] != 0x67 || data[1] != 0x67 || data[2] != 0x03)
            throw new ArgumentException("Invalid heartbeat packet header");

        int size = (data[3] << 8) | data[4];
        if (data.Length < 5 + size)
            throw new ArgumentException("Packet size mismatch");

        int offset = 5;

        // Sequence: 2 bytes
        ushort sequence = (ushort)((data[offset] << 8) | data[offset + 1]);
        offset += 2;

        // Status: 2 bytes
        ushort statusWord = (ushort)((data[offset] << 8) | data[offset + 1]);

        return new Heartbeat
        {
            Sequence = sequence,
            Status = Status.Parse(statusWord)
        };
    }

    public override string ToString()
    {
        return $"Heartbeat: Sequence={Sequence:X4}, Status={Status}";
    }
}
