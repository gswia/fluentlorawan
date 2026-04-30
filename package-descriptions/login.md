# Login Package (0x01)

If using TCP, login package will be sent to server immediately after every session is established. If using UDP, it will be sent only once after the device is rebooted. The server must respond it, or all other packages will not be sent.

## Device → Server Structure

| Name | Bytes | Description |
|---|---|---|
| Mark | 2 | 0x67 0x67 |
| PID | 1 | Package identifier — 0x01 |
| Size | 2 | Package size from next byte to end — Unsigned 16 bits integer |
| Sequence | 2 | Package sequence number — Unsigned 16 bits integer |
| IMEI | 8 | Device IMEI |
| Language | 1 | Device language: 0x00 — Chinese; 0x01 — English; Other — Undefined |
| Timezone | 1 | Device timezone — Signed 8 bits integer (in 15 mins) |
| Sys Ver | 2 | System version — Unsigned 16 bits integer (e.g. 0x0205: V2.0.5) |
| App Ver | 2 | Application version — Unsigned 16 bits integer (e.g. 0x0205: V2.0.5) |
| PS Ver | 2 | Param-set (see note 1) version — Unsigned 16 bits integer (e.g. 0x0001: V1) |
| PS OSize | 2 | Param-set original size — Unsigned 16 bits integer |
| PS CSize | 2 | Param-set compressed size (see note 2) — Unsigned 16 bits integer |
| PS Sum16 | 2 | Param-set checksum (see note 3) — Unsigned 16 bits integer |

**Note:**
1. Param-set is the set of all parameters, which is described in Appendix A.3 PARAM-SET.
2. The deflate algorithm is described in Appendix A.2 DEFLATE ALGORITHM.
3. The checksum algorithm is described in Appendix A.1 CHECKSUM ALGORITHM.

## Server → Device Response

| Name | Bytes | Description |
|---|---|---|
| Mark | 2 | 0x67 0x67 |
| PID | 1 | Package identifier — 0x01 |
| Size | 2 | Package size from next byte to end — Unsigned 16 bits integer |
| Sequence | 2 | Package sequence number — Unsigned 16 bits integer |
| Time | 4 | Current time (UTC) in the server, see [time.md](time.md) |
| Version | 2 | Protocol version (see note 1) — 0x01: default |
| PS Action | 1 | Param-set action mask (see note 2) |

**Note:**
1. "Protocol version" is the version of the protocol supported by server. If it is different from the protocol version in device, device will generate and transmit only the compatible packages to server.
   - 0x01: The server doesn't support any extension (in compatible mode)
   - 0x02: The server supports POSITION extension V2 (see [position.md](position.md))
   - 0x03: The server supports POSITION* extension V3 (see [position.md](position.md))

2. When server receives the login package from device, it can check the information of Param-set to determine how to operates the Param-set. Then 2 optional actions may be taken and both of them can be taken at the same time:
   - Bit0: 1 — Tell device to upload the Param-set immediately. 0 — Do not upload it now.
   - Bit1: 1 — Tell device to upload the Param-set if changed in the future. 0 — Do not upload it in the future.
