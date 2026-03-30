# Location Package (0x12)

Location package is the most important package. It transfers the position and other information of device to server. If using TCP, the server need not respond it. If using UDP, the server must respond it.

## Device → Server Structure

| Name | Bytes | Description |
|---|---|---|
| Mark | 2 | 0x67 0x67 |
| PID | 1 | Package identifier — 0x12 |
| Size | 2 | Package size from next byte to end — Unsigned 16 bits integer |
| Sequence | 2 | Package sequence number — Unsigned 16 bits integer |
| Location | 5+N | Device position, see [position.md](position.md) |
| Hardware | 20 | Hardware data, see [hardware.md](hardware.md) |
| Sensors | 14 | Sensors data, see [sensors.md](sensors.md) |
| Beacons | 2+16N | Beacon data, see [beacon.md](beacon.md) |

**Note:**
1. Not all fields are valid in a device, so please ignore non-existent or unnecessary data. For example: sensor data are valid only if relevant sensors exist. If a datum is invalid, its value will be zero.

## Server → Device Response

| Name | Bytes | Description |
|---|---|---|
| Mark | 2 | 0x67 0x67 |
| PID | 1 | Package identifier — 0x12 |
| Size | 2 | Package size from next byte to end — Unsigned 16 bits integer |
| Sequence | 2 | Package sequence number — Unsigned 16 bits integer |
