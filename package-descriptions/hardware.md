# Hardware

Hardware is a compound data type. It contains all data related to hardware, e.g. battery, etc.

## Structure

| Name | Bytes | Description |
|---|---|---|
| Status | 2 | Device status, see [status.md](status.md) |
| Battery | 2 | Battery voltage (in mV) — Unsigned 16 bits integer |
| AIN0 | 2 | AIN0 value (in mV) — Unsigned 16 bits integer |
| AIN1 | 2 | AIN1 value (in mV) — Unsigned 16 bits integer |
| Mileage | 4 | Device mileage (in m) — Unsigned 32 bits integer |
| GSM Cntr | 2 | GSM counter from last GSM command (in min) — Unsigned 16 bits integer |
| GPS Cntr | 2 | GPS counter from last GPS command (in min) — Unsigned 16 bits integer |
| PDM Step | 2 | Accumulated steps today — Unsigned 16 bits integer |
| PDM Time | 2 | Accumulated walking time today (in sec) — Unsigned 16 bits integer |

**Note:**
1. AIN is the abbreviation of analog input port.
2. Mileage is accumulated only when GPS is fixed.
3. GSM Counter is used to recognize which phase GSM module is in. The phases are described in the GSM command (see [commands.md](../commands.md)).
4. GPS Counter is used to recognize which phase GPS module is in. The phases are described in the GPS command (see [commands.md](../commands.md)).
