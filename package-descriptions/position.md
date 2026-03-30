# Position

Position is a compound data type. It contains all data related to location, e.g. latitude, longitude, altitude, GSM BSID, WLAN BSSID, etc. In order to get minimum length, it is defined in variable size. It contains only valid data and eliminates invalid one. A mask is used to indicate which data are valid.

There are 3 versions of Position. Higher version is always compatible with a lower one. The server reports its supported version via login package (see [login.md](login.md)).

## Basic Fields

| Name | Bytes | Description |
|---|---|---|
| Time | 4 | The event time (UTC) when position data is collected, see [time.md](time.md) |
| Mask | 1 | The mask to indicate which data are valid (see below) |

### Mask Bits

| Bit | Description |
|---|---|
| Bit 0 | GPS data is valid |
| Bit 1 | BSID0 data is valid |
| Bit 2 | BSID1 data is valid |
| Bit 3 | BSID2 data is valid |
| Bit 4 | BSS0 data is valid |
| Bit 5 | BSS1 data is valid |
| Bit 6 | BSS2 data is valid |
| Bit 7 | EXT data is valid |

## GPS Data

GPS is the GPS data.

| Name | Bytes | Description |
|---|---|---|
| Latitude | 4 | -90.0 ~ 90.0 degree — Signed 32 bits integer from -162000000 to 162000000 (in 1/500") |
| Longitude | 4 | -180.0 ~ 180.0 degree — Signed 32 bits integer from -324000000 to 324000000 (in 1/500") |
| Altitude | 2 | Signed 16 bits integer from -32768 to 32767 (in meters) |
| Speed | 2 | Unsigned 16 bits integer (in km/h) |
| Course | 2 | Unsigned 16 bits integer from 0 to 360 (in degrees) |
| Satellites | 1 | The number of satellites |

## BSID0 (2G/3G Serving Cell)

BSID0 is the info of the 2G/3G serving cell.

| Name | Bytes | Description |
|---|---|---|
| MCC | 2 | Mobile Country Code — Unsigned 16 bits integer |
| MNC | 2 | Mobile Network Code — Unsigned 16 bits integer |
| LAC | 2 | Location Area Code — Unsigned 16 bits integer |
| CID | 4 | Cell ID — Unsigned 32 bits integer |
| RxLev | 1 | Cell signal level — Unsigned 8 bits integer (0: -111dB 1:-110dB 2:-109dB ... 111: 0dB) |

## BSID1 and BSID2 (2G/3G Neighbor Cells)

BSID1 and BSID2 are the info of two 2G/3G neighbor cells. Each of them contains:

| Name | Bytes | Description |
|---|---|---|
| LAC | 2 | Same as definition in BSID0 |
| CI | 4 | Same as definition in BSID0 |
| RxLev | 1 | Same as definition in BSID0 |

## BSS0, BSS1, BSS2 (WiFi Hotspots)

BSS0, BSS1 and BSS2 are the info of three WiFi hotspots. Each of them contains:

| Name | Bytes | Description |
|---|---|---|
| BSSID | 6 | WiFi MAC address |
| RSSI | 1 | WiFi signal power — Signed 8 bits integer (in dB) |

## EXT Fields

| Name | Bytes | Description |
|---|---|---|
| RAT | 1 | Radio access technology (see below) |
| NoC | 1 | Number of cells (see below) |

### RAT (Radio Access Technology)

| Value | Description |
|---|---|
| 0 | 2G GSM |
| 1 | 2G GSM Compact |
| 2 | 3G UTRAN |
| 3 | 2G GSM EDGE |
| 4 | 3G UTRAN HSDPA |
| 5 | 3G UTRAN HSUPA |
| 6 | 3G UTRAN HSDPA & HSUPA |
| 7 | LTE |
| 8 | LTE CAT-M1 |
| 9 | LTE NB-IoT |

### NoC (Number of Cells)

| Bit | Description |
|---|---|
| Bit 2~0 | Number of LTE cells |
| Bit 6~3 | Number of WiFi hotspots |
| Bit 7 | Reserved for future use |

## EXT LTE-SRV (LTE Serving Cell)

EXT LTE-SRV is the info of the LTE serving cell.

| Name | Bytes | Description |
|---|---|---|
| MCC | 2 | Mobile Country Code — Unsigned 16 bits integer |
| MNC | 2 | Mobile Network Code — Unsigned 16 bits integer |
| LAC | 2 | Location Area Code — Unsigned 16 bits integer |
| TAC | 2 | Tracking Area Code — Unsigned 16 bits integer |
| CID | 4 | Cell ID — Unsigned 32 bits integer |
| TA | 2 | Timing Advance — Unsigned 16 bits integer |
| PCID | 2 | Physical Cell ID — Unsigned 16 bits integer |
| EARFCN | 2 | E-ARFCN — Unsigned 16 bits integer |
| RSRP | 1 | Reference Signal Received Power — Signed 8 bits integer (in dB) |

## EXT LTE-NBR (LTE Neighbor Cells)

EXT LTE-NBR are the info of the LTE neighbor cells. It repeats for (NoC:Bit2~0 - 1) times. Each of them contains:

| Name | Bytes | Description |
|---|---|---|
| PCID | 2 | Same as definition in LTE-SRV |
| EARFCN | 2 | Same as definition in LTE-SRV |
| RSRP | 1 | Same as definition in LTE-SRV |

## EXT HOTSPOT (WiFi Hotspots Extended)

EXT HOTSPOT are the info of all WiFi hotspots. It repeats for (NoC:Bit6~3) times. Each of them contains:

| Name | Bytes | Description |
|---|---|---|
| BSSID | 6 | WiFi MAC address |
| RSSI | 1 | WiFi signal power — Signed 8 bits integer (in dB) |
