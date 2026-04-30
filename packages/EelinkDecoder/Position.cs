namespace EelinkDecoder;

/// <summary>
/// GPS data (13 bytes).
/// </summary>
public class GpsData
{
    /// <summary>Latitude in degrees (-90.0 to 90.0).</summary>
    public double Latitude { get; set; }

    /// <summary>Longitude in degrees (-180.0 to 180.0).</summary>
    public double Longitude { get; set; }

    /// <summary>Altitude in meters.</summary>
    public short Altitude { get; set; }

    /// <summary>Speed in km/h.</summary>
    public ushort Speed { get; set; }

    /// <summary>Course in degrees (0-360).</summary>
    public ushort Course { get; set; }

    /// <summary>Number of satellites.</summary>
    public byte Satellites { get; set; }

    public static GpsData Parse(byte[] data, ref int offset)
    {
        int lat = (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];
        int lon = (data[offset + 4] << 24) | (data[offset + 5] << 16) | (data[offset + 6] << 8) | data[offset + 7];
        short alt = (short)((data[offset + 8] << 8) | data[offset + 9]);
        ushort speed = (ushort)((data[offset + 10] << 8) | data[offset + 11]);
        ushort course = (ushort)((data[offset + 12] << 8) | data[offset + 13]);
        byte sats = data[offset + 14];

        offset += 15;

        return new GpsData
        {
            Latitude = lat / 1800000.0,  // 1/500" to degrees
            Longitude = lon / 1800000.0,
            Altitude = alt,
            Speed = speed,
            Course = course,
            Satellites = sats
        };
    }
}

/// <summary>
/// 2G/3G Serving Cell (11 bytes).
/// </summary>
public class Bsid0
{
    public ushort MCC { get; set; }
    public ushort MNC { get; set; }
    public ushort LAC { get; set; }
    public uint CID { get; set; }
    public byte RxLev { get; set; }

    public static Bsid0 Parse(byte[] data, ref int offset)
    {
        ushort mcc = (ushort)((data[offset] << 8) | data[offset + 1]);
        ushort mnc = (ushort)((data[offset + 2] << 8) | data[offset + 3]);
        ushort lac = (ushort)((data[offset + 4] << 8) | data[offset + 5]);
        uint cid = (uint)((data[offset + 6] << 24) | (data[offset + 7] << 16) | (data[offset + 8] << 8) | data[offset + 9]);
        byte rxLev = data[offset + 10];

        offset += 11;

        return new Bsid0 { MCC = mcc, MNC = mnc, LAC = lac, CID = cid, RxLev = rxLev };
    }
}

/// <summary>
/// 2G/3G Neighbor Cell (7 bytes).
/// </summary>
public class BsidNeighbor
{
    public ushort LAC { get; set; }
    public uint CI { get; set; }
    public byte RxLev { get; set; }

    public static BsidNeighbor Parse(byte[] data, ref int offset)
    {
        ushort lac = (ushort)((data[offset] << 8) | data[offset + 1]);
        uint ci = (uint)((data[offset + 2] << 24) | (data[offset + 3] << 16) | (data[offset + 4] << 8) | data[offset + 5]);
        byte rxLev = data[offset + 6];

        offset += 7;

        return new BsidNeighbor { LAC = lac, CI = ci, RxLev = rxLev };
    }
}

/// <summary>
/// WiFi Hotspot (7 bytes).
/// </summary>
public class Bss
{
    public byte[] BSSID { get; set; } = new byte[6];
    public sbyte RSSI { get; set; }

    public static Bss Parse(byte[] data, ref int offset)
    {
        var bssid = new byte[6];
        Array.Copy(data, offset, bssid, 0, 6);
        sbyte rssi = (sbyte)data[offset + 6];

        offset += 7;

        return new Bss { BSSID = bssid, RSSI = rssi };
    }
}

/// <summary>
/// LTE Serving Cell (19 bytes).
/// </summary>
public class LteServingCell
{
    public ushort MCC { get; set; }
    public ushort MNC { get; set; }
    public ushort LAC { get; set; }
    public ushort TAC { get; set; }
    public uint CID { get; set; }
    public ushort TA { get; set; }
    public ushort PCID { get; set; }
    public ushort EARFCN { get; set; }
    public sbyte RSRP { get; set; }

    public static LteServingCell Parse(byte[] data, ref int offset)
    {
        ushort mcc = (ushort)((data[offset] << 8) | data[offset + 1]);
        ushort mnc = (ushort)((data[offset + 2] << 8) | data[offset + 3]);
        ushort lac = (ushort)((data[offset + 4] << 8) | data[offset + 5]);
        ushort tac = (ushort)((data[offset + 6] << 8) | data[offset + 7]);
        uint cid = (uint)((data[offset + 8] << 24) | (data[offset + 9] << 16) | (data[offset + 10] << 8) | data[offset + 11]);
        ushort ta = (ushort)((data[offset + 12] << 8) | data[offset + 13]);
        ushort pcid = (ushort)((data[offset + 14] << 8) | data[offset + 15]);
        ushort earfcn = (ushort)((data[offset + 16] << 8) | data[offset + 17]);
        sbyte rsrp = (sbyte)data[offset + 18];

        offset += 19;

        return new LteServingCell
        {
            MCC = mcc, MNC = mnc, LAC = lac, TAC = tac, CID = cid,
            TA = ta, PCID = pcid, EARFCN = earfcn, RSRP = rsrp
        };
    }
}

/// <summary>
/// LTE Neighbor Cell (5 bytes).
/// </summary>
public class LteNeighborCell
{
    public ushort PCID { get; set; }
    public ushort EARFCN { get; set; }
    public sbyte RSRP { get; set; }

    public static LteNeighborCell Parse(byte[] data, ref int offset)
    {
        ushort pcid = (ushort)((data[offset] << 8) | data[offset + 1]);
        ushort earfcn = (ushort)((data[offset + 2] << 8) | data[offset + 3]);
        sbyte rsrp = (sbyte)data[offset + 4];

        offset += 5;

        return new LteNeighborCell { PCID = pcid, EARFCN = earfcn, RSRP = rsrp };
    }
}

/// <summary>
/// Position compound data type (variable length).
/// Contains GPS, cell tower, and WiFi positioning data based on mask.
/// </summary>
public class Position
{
    public DateTime Time { get; set; }
    public byte Mask { get; set; }

    public GpsData? Gps { get; set; }
    public Bsid0? Bsid0 { get; set; }
    public BsidNeighbor? Bsid1 { get; set; }
    public BsidNeighbor? Bsid2 { get; set; }
    public Bss? Bss0 { get; set; }
    public Bss? Bss1 { get; set; }
    public Bss? Bss2 { get; set; }

    // EXT fields
    public byte? RAT { get; set; }
    public byte? NoC { get; set; }
    public LteServingCell? LteServing { get; set; }
    public LteNeighborCell[]? LteNeighbors { get; set; }
    public Bss[]? HotspotsExt { get; set; }

    /// <summary>Parse Position from byte array at specified offset (big-endian).</summary>
    public static Position Parse(byte[] data, ref int offset)
    {
        // Time: Unix timestamp (4 bytes)
        uint timeStamp = (uint)((data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3]);
        DateTime time = DateTimeOffset.FromUnixTimeSeconds(timeStamp).UtcDateTime;
        offset += 4;

        // Mask: 1 byte
        byte mask = data[offset];
        offset += 1;

        var position = new Position { Time = time, Mask = mask };

        // GPS (bit 0)
        if ((mask & 0x01) != 0)
            position.Gps = GpsData.Parse(data, ref offset);

        // BSID0 (bit 1)
        if ((mask & 0x02) != 0)
            position.Bsid0 = Bsid0.Parse(data, ref offset);

        // BSID1 (bit 2)
        if ((mask & 0x04) != 0)
            position.Bsid1 = BsidNeighbor.Parse(data, ref offset);

        // BSID2 (bit 3)
        if ((mask & 0x08) != 0)
            position.Bsid2 = BsidNeighbor.Parse(data, ref offset);

        // BSS0 (bit 4)
        if ((mask & 0x10) != 0)
            position.Bss0 = Bss.Parse(data, ref offset);

        // BSS1 (bit 5)
        if ((mask & 0x20) != 0)
            position.Bss1 = Bss.Parse(data, ref offset);

        // BSS2 (bit 6)
        if ((mask & 0x40) != 0)
            position.Bss2 = Bss.Parse(data, ref offset);

        // EXT (bit 7)
        if ((mask & 0x80) != 0)
        {
            position.RAT = data[offset];
            position.NoC = data[offset + 1];
            offset += 2;

            byte lteCellCount = (byte)(position.NoC.Value & 0x07);      // Bits 2-0
            byte wifiCount = (byte)((position.NoC.Value >> 3) & 0x0F);  // Bits 6-3

            // LTE Serving Cell (always present if any LTE cells)
            if (lteCellCount > 0)
            {
                position.LteServing = LteServingCell.Parse(data, ref offset);

                // LTE Neighbor Cells (lteCellCount - 1)
                if (lteCellCount > 1)
                {
                    position.LteNeighbors = new LteNeighborCell[lteCellCount - 1];
                    for (int i = 0; i < lteCellCount - 1; i++)
                        position.LteNeighbors[i] = LteNeighborCell.Parse(data, ref offset);
                }
            }

            // WiFi Hotspots Extended
            if (wifiCount > 0)
            {
                position.HotspotsExt = new Bss[wifiCount];
                for (int i = 0; i < wifiCount; i++)
                    position.HotspotsExt[i] = Bss.Parse(data, ref offset);
            }
        }

        return position;
    }
}
