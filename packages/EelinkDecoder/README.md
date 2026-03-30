# Eelink Packet Decoder

Console application for decoding Eelink GPT45 binary payloads.

## Usage

```bash
cd c:\iothubtest\packages\EelinkDecoder
dotnet run -- <hex_payload>
```

## Examples

### Decode a 0x12 GPS location packet
```bash
dotnet run -- 676712001A00010B21D7C90358E6580BF8F99E010F003C2800
```

### Decode from App Insights PayloadHex
Copy the `payload_hex` value from Application Insights and paste:
```bash
dotnet run -- "00010B21D7C90358E6580BF8F99E010F003C2800"
```

## Building

```bash
dotnet build
dotnet publish -c Release
```

## Output

The decoder will show:
- Packet type (0x12 = location, 0x14 = alarm, etc.)
- Sequence number
- Alarm type (if 0x14 packet)
- GPS timestamp, latitude, longitude
- Speed and course
- All 16 status bits decoded with descriptions

## Supported Packet Types

- **0x12** - GPS location
- **0x14** - Alarm (motion, speed, fence, etc.)

More packet types will be added as needed.
