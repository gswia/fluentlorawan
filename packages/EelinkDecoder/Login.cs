using System.Text;

namespace EelinkDecoder;

/// <summary>
/// Login Package (0x01) - Device → Server.
/// Sent immediately after TCP session is established.
/// </summary>
public class Login
{
    /// <summary>Package sequence number.</summary>
    public ushort Sequence { get; set; }

    /// <summary>Device IMEI (15 digits).</summary>
    public string IMEI { get; set; } = string.Empty;

    /// <summary>Device language (0x00: Chinese, 0x01: English).</summary>
    public byte Language { get; set; }

    /// <summary>Device timezone offset in 15-minute increments (signed).</summary>
    public sbyte Timezone { get; set; }

    /// <summary>System version (e.g. 0x0205 = V2.0.5).</summary>
    public ushort SystemVersion { get; set; }

    /// <summary>Application version (e.g. 0x0205 = V2.0.5).</summary>
    public ushort AppVersion { get; set; }

    /// <summary>Param-set version.</summary>
    public ushort ParamSetVersion { get; set; }

    /// <summary>Param-set original size.</summary>
    public ushort ParamSetOriginalSize { get; set; }

    /// <summary>Param-set compressed size.</summary>
    public ushort ParamSetCompressedSize { get; set; }

    /// <summary>Param-set checksum.</summary>
    public ushort ParamSetChecksum { get; set; }

    /// <summary>Parse Login packet from complete packet bytes (including 67 67 header).</summary>
    public static Login Parse(byte[] data)
    {
        // Verify header
        if (data.Length < 5 || data[0] != 0x67 || data[1] != 0x67 || data[2] != 0x01)
            throw new ArgumentException("Invalid login packet header");

        int size = (data[3] << 8) | data[4];
        if (data.Length < 5 + size)
            throw new ArgumentException("Packet size mismatch");

        int offset = 5;

        // Sequence: 2 bytes
        ushort sequence = (ushort)((data[offset] << 8) | data[offset + 1]);
        offset += 2;

        // IMEI: 8 bytes BCD (15 digits + padding)
        string imei = DecodeBcdImei(data, offset, 8);
        offset += 8;

        // Language: 1 byte
        byte language = data[offset++];

        // Timezone: 1 byte signed (in 15 min increments)
        sbyte timezone = (sbyte)data[offset++];

        // System Version: 2 bytes
        ushort sysVer = (ushort)((data[offset] << 8) | data[offset + 1]);
        offset += 2;

        // App Version: 2 bytes
        ushort appVer = (ushort)((data[offset] << 8) | data[offset + 1]);
        offset += 2;

        // Param-set Version: 2 bytes
        ushort psVer = (ushort)((data[offset] << 8) | data[offset + 1]);
        offset += 2;

        // Param-set Original Size: 2 bytes
        ushort psOSize = (ushort)((data[offset] << 8) | data[offset + 1]);
        offset += 2;

        // Param-set Compressed Size: 2 bytes
        ushort psCSize = (ushort)((data[offset] << 8) | data[offset + 1]);
        offset += 2;

        // Param-set Checksum: 2 bytes
        ushort psSum16 = (ushort)((data[offset] << 8) | data[offset + 1]);

        return new Login
        {
            Sequence = sequence,
            IMEI = imei,
            Language = language,
            Timezone = timezone,
            SystemVersion = sysVer,
            AppVersion = appVer,
            ParamSetVersion = psVer,
            ParamSetOriginalSize = psOSize,
            ParamSetCompressedSize = psCSize,
            ParamSetChecksum = psSum16
        };
    }

    /// <summary>Decode BCD-encoded IMEI (8 bytes = 15 digits + padding).</summary>
    private static string DecodeBcdImei(byte[] data, int offset, int count)
    {
        var sb = new StringBuilder(count * 2);
        for (int i = 0; i < count; i++)
        {
            sb.Append((data[offset + i] >> 4) & 0x0F);
            sb.Append(data[offset + i] & 0x0F);
        }
        return sb.ToString().TrimEnd('F', 'f');
    }

    /// <summary>Format version number (0x0205 → "2.0.5").</summary>
    public static string FormatVersion(ushort version)
    {
        int major = (version >> 8) & 0xFF;
        int minor = (version >> 4) & 0x0F;
        int patch = version & 0x0F;
        return $"{major}.{minor}.{patch}";
    }

    public override string ToString()
    {
        return $"Login: IMEI={IMEI}, SysVer={FormatVersion(SystemVersion)}, AppVer={FormatVersion(AppVersion)}";
    }
}
