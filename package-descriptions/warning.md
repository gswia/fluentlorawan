# Warning Package (0x14)

A warning package will be sent to server when a specific warning occurs.

## Device → Server Structure

| Name | Bytes | Description |
|---|---|---|
| Mark | 2 | 0x67 0x67 |
| PID | 1 | Package identifier — 0x14 |
| Size | 2 | Package size from next byte to end — Unsigned 16 bits integer |
| Sequence | 2 | Package sequence number — Unsigned 16 bits integer |
| Location | 5+N | Device position, see [position.md](position.md) |
| Warning | 1 | Warning type — Unsigned 8 bits integer |
| Status | 2 | Device status, see [status.md](status.md) |
| CData | N | Custom data, see below |

## Warning Types

| Value | Description |
|---|---|
| 0x02 | SOS |
| 0x01 | External power cut-off |
| 0x03 | Battery low (only for the device which uses a battery as main power) |
| 0x08 | GPS antenna open-circuit (only for the device with external GPS antenna) |
| 0x09 | GPS antenna short-circuit (only for the device with external GPS antenna) |
| 0x04 | Activity warning (only for the device with an accelerometer) |
| 0x85 | Shock warning (only for the device with an accelerometer) |
| 0x86 | Free-fall warning (only for the device with an accelerometer) |
| 0x87 | Tilt warning (only for the device with an accelerometer) |
| 0x82 | Under/over speed warning |
| 0x83 | In-to-fence warning |
| 0x84 | Out-of-fence warning |
| 0x05 | Shift warning |
| 0x20 | Out of internal temperature range |
| 0x21 | Out of humidity range |
| 0x22 | Out of illuminance range |
| 0x23 | Out of CO2 concentration range |
| 0x24 | Out of probe temperature range |
| 0x25 | Light wakeup warning (only for supported models) |
| 0x26 | Motion wakeup warning (only for supported models) |

## Custom Data for Battery Warning Package

| Name | Bytes | Description |
|---|---|---|
| Power | 2 | External power voltage (in mV) — Unsigned 16 bits integer |
| Battery | 2 | Internal battery voltage (in mV) — Unsigned 16 bits integer |

## Custom Data for Shock/Freefall/Tilt Warning Package

| Name | Bytes | Description |
|---|---|---|
| g.x | 2 | X of the acceleration vector (in (1/256)g) — Signed 16 bits integer |
| g.y | 2 | Y of the acceleration vector (in (1/256)g) — Signed 16 bits integer |
| g.z | 2 | Z of the acceleration vector (in (1/256)g) — Signed 16 bits integer |

## Custom Data for Speed Warning Package

| Name | Bytes | Description |
|---|---|---|
| Speed | 2 | Current speed (in km/h) — Unsigned 16 bits integer |
| Low | 2 | The low limit of the speed (in km/h) — Unsigned 16 bits integer |
| High | 2 | The high limit of the speed (in km/h) — Unsigned 16 bits integer |

## Custom Data for All Sensors' Warning Packages

| Name | Bytes | Description |
|---|---|---|
| Value | 4 | Current value — Signed 32 bits integer |
| Low | 4 | The low limit of the sensor — Signed 32 bits integer |
| High | 4 | The high limit of the sensor — Signed 32 bits integer |

**Note:**
Sensor value and limit have different units with different sensor type:
- Internal temperature: in (1/256)°C
- Humidity: in (1/10)%
- Illuminance: in (1/256)lx
- CO2 concentration: in ppm
- Probe temperature: in (1/16)°C

## Server → Device Response

| Name | Bytes | Description |
|---|---|---|
| Mark | 2 | 0x67 0x67 |
| PID | 1 | Package identifier — 0x14 |
| Size | 2 | Package size from next byte to end — Unsigned 16 bits integer |
| Sequence | 2 | Package sequence number — Unsigned 16 bits integer |
| Content | N | Warning content — String |

**Note:**
1. Warning content has variable length. Its length is the size of the body.
2. If warning content is not empty, it will be sent as a message to all managers registered in device.
