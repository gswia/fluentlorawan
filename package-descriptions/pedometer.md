# Pedometer Package (0x1A)

If device has an accelerometer, it will attempt to analyze the acceleration data as a pedometer. The result will be sent to server per a day.

## Device → Server Structure

| Name | Bytes | Description |
|---|---|---|
| Mark | 2 | 0x67 0x67 |
| PID | 1 | Package identifier — 0x1A |
| Size | 2 | Package size from next byte to end — Unsigned 16 bits integer |
| Sequence | 2 | Package sequence number — Unsigned 16 bits integer |
| Today | 4 | UTC time of last day, see [time.md](time.md) |
| All Step | 4 | Accumulated steps in total — Unsigned 32 bits integer |
| All Time | 4 | Accumulated walking time in total (in sec) — Unsigned 32 bits integer |
| All Distance | 4 | Accumulated walking distance in total (in mm) — Unsigned 32 bits integer |
| All Energy | 4 | Accumulated burnt energy in total (in cal) — Unsigned 32 bits integer |
| Day Step | 4 | Accumulated steps in last day — Unsigned 32 bits integer |
| Day Time | 4 | Accumulated walking time in last day (in sec) — Unsigned 32 bits integer |
| Day Distance | 4 | Accumulated walking distance in last day (in mm) — Unsigned 32 bits integer |
| Day Energy | 4 | Accumulated burnt energy in last day (in cal) — Unsigned 32 bits integer |

## Server → Device Response

| Name | Bytes | Description |
|---|---|---|
| Mark | 2 | 0x67 0x67 |
| PID | 1 | Package identifier — 0x1A |
| Size | 2 | Package size from next byte to end — Unsigned 16 bits integer |
| Sequence | 2 | Package sequence number — Unsigned 16 bits integer |
