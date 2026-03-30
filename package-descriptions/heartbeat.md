# Heartbeat Package (0x03)

Heartbeat package only appears in TCP. It is used to keep the session active. In common situation, if nothing are sent via the pathway (GPRS) in a few minutes, the pathway may be recycled by network provider. So heartbeat package must be sent to keep the session active when it is nearly due.

If device has sent some heartbeat packages and not received any response, it will cut the corrupt connection and attempt to establish a new one.

UDP is not a stream-like protocol, so heartbeat package is not necessary for it.

## Device → Server Structure

| Name | Bytes | Description |
|---|---|---|
| Mark | 2 | 0x67 0x67 |
| PID | 1 | Package identifier — 0x03 |
| Size | 2 | Package size from next byte to end — Unsigned 16 bits integer |
| Sequence | 2 | Package sequence number — Unsigned 16 bits integer |
| Status | 2 | Device status, see [status.md](status.md) |

## Server → Device Response

| Name | Bytes | Description |
|---|---|---|
| Mark | 2 | 0x67 0x67 |
| PID | 1 | Package identifier — 0x03 |
| Size | 2 | Package size from next byte to end — Unsigned 16 bits integer |
| Sequence | 2 | Package sequence number — Unsigned 16 bits integer |
