namespace EelinkDecoder;

/// <summary>
/// Represents the 16-bit device status word as defined in the protocol spec.
/// </summary>
public class Status
{
    /// <summary>Bit 0: GPS is fixed.</summary>
    public bool GpsFixed { get; set; }

    /// <summary>Bit 1: Device is designed for car.</summary>
    public bool IsCarDevice { get; set; }

    /// <summary>Bit 2: Car engine is fired. Only valid when IsCarDevice is true.</summary>
    public bool EngineOn { get; set; }

    /// <summary>Bit 3: Accelerometer is supported.</summary>
    public bool HasAccelerometer { get; set; }

    /// <summary>Bit 4: Motion warning is activated. Only valid when HasAccelerometer is true.</summary>
    public bool MotionWarningActive { get; set; }

    /// <summary>Bit 5: Relay control is supported.</summary>
    public bool HasRelayControl { get; set; }

    /// <summary>Bit 6: Relay control is triggered. Only valid when HasRelayControl is true.</summary>
    public bool RelayTriggered { get; set; }

    /// <summary>Bit 7: External charging is supported.</summary>
    public bool HasExternalCharging { get; set; }

    /// <summary>Bit 8: Device is charging. Only valid when HasExternalCharging is true.</summary>
    public bool IsCharging { get; set; }

    /// <summary>Bit 9: Device is active (moving). Only valid when HasAccelerometer is true.</summary>
    public bool IsActive { get; set; }

    /// <summary>Bit 10: GPS module is running.</summary>
    public bool GpsRunning { get; set; }

    /// <summary>Bit 11: OBD module is running. Only valid when OBD is supported.</summary>
    public bool ObdRunning { get; set; }

    /// <summary>Bit 12: DIN0 is high level. Only valid when DIN0 is supported.</summary>
    public bool Din0High { get; set; }

    /// <summary>Bit 13: DIN1 is high level. Only valid when DIN1 is supported.</summary>
    public bool Din1High { get; set; }

    /// <summary>Bit 14: DIN2 is high level. Only valid when DIN2 is supported.</summary>
    public bool Din2High { get; set; }

    /// <summary>Bit 15: DIN3 is high level. Only valid when DIN3 is supported.</summary>
    public bool Din3High { get; set; }

    /// <summary>Parse Status from byte array at specified offset (big-endian).</summary>
    public static Status Parse(byte[] data, int offset)
    {
        ushort value = (ushort)((data[offset] << 8) | data[offset + 1]);
        return Parse(value);
    }

    public static Status Parse(ushort value) => new Status
    {
        GpsFixed             = (value & (1 << 0))  != 0,
        IsCarDevice          = (value & (1 << 1))  != 0,
        EngineOn             = (value & (1 << 2))  != 0,
        HasAccelerometer     = (value & (1 << 3))  != 0,
        MotionWarningActive  = (value & (1 << 4))  != 0,
        HasRelayControl      = (value & (1 << 5))  != 0,
        RelayTriggered       = (value & (1 << 6))  != 0,
        HasExternalCharging  = (value & (1 << 7))  != 0,
        IsCharging           = (value & (1 << 8))  != 0,
        IsActive             = (value & (1 << 9))  != 0,
        GpsRunning           = (value & (1 << 10)) != 0,
        ObdRunning           = (value & (1 << 11)) != 0,
        Din0High             = (value & (1 << 12)) != 0,
        Din1High             = (value & (1 << 13)) != 0,
        Din2High             = (value & (1 << 14)) != 0,
        Din3High             = (value & (1 << 15)) != 0,
    };
}
