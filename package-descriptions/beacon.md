# Beacon

Beacon is a compound data type. It contains all data related to BLE/Beacon, e.g. address, signal level, temperature, etc.

## Beacons Structure

The structure of all beacons is described as below:

| Name | Bytes | Description |
|---|---|---|
| Number | 1 | Beacon number — Unsigned 8 bits integer |
| ID | 1 | Data ID (0x00: No BLE; 0x02: With Beacon) |
| Beacons | 16N | Data content (N: beacon number and 16 bytes data per beacon) |

## Single Beacon Structure

The structure of a beacon is described as below:

| Name | Bytes | Description |
|---|---|---|
| BLEID | 6 | BLE address (reverse order, the last byte is the first address) |
| RSSI | 1 | Signal level (in dB) — Signed 8 bits integer |
| Reserved | 1 | 0x00 |
| Model | 1 | Beacon model (e.g. 0xB1, 0xF2, 0x29) |
| Version | 1 | Beacon Version |
| Battery | 2 | Battery voltage (in mV) — Unsigned 16 bits integer |
| Temperature | 2 | Sensor temperature (in (1/256)°C) — Signed 16 bits integer |
| Self-Defined | 2 | Self defined datum by beacon (see its specification) |
