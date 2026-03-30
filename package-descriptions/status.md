# Status

A unsigned 16 bits integer is used to represent the status of device. The definition of each bits are described as listed below:

| Bit | Value | Description |
|-----|-------|-------------|
| 0 | 1 | GPS is fixed |
|   | 0 | GPS is not fixed |
| 1 | 1 | Device is designed for car |
|   | 0 | Device is not designed for car |
| 2 | 1 | Car engine is fired (only valid when bit 1 is 1) |
|   | 0 | Car engine is not fired |
| 3 | 1 | Accelerometer is supported |
|   | 0 | No accelerometer |
| 4 | 1 | The motion-warning is activated (only valid when bit 3 is 1) |
|   | 0 | The motion-warning is deactivated |
| 5 | 1 | Relay control is supported |
|   | 0 | No relay control |
| 6 | 1 | The relay control is triggered (only valid when bit 5 is 1) |
|   | 0 | The relay control is not triggered |
| 7 | 1 | External charging is supported |
|   | 0 | No external charging |
| 8 | 1 | Device is charging (only valid when bit 7 is 1) |
|   | 0 | Device is not charging |
| 9 | 1 | Device is active (only valid when bit 3 is 1) |
|   | 0 | Device is stationary |
| 10 | 1 | GPS module is running |
|    | 0 | GPS module is not running |
| 11 | 1 | OBD module is running (only valid when OBD is supported) |
|    | 0 | OBD module is not running |
| 12 | 1 | DIN0 is high level (only valid when DIN0 is supported) |
|    | 0 | DIN0 is low level |
| 13 | 1 | DIN1 is high level (only valid when DIN1 is supported) |
|    | 0 | DIN1 is low level |
| 14 | 1 | DIN2 is high level (only valid when DIN2 is supported) |
|    | 0 | DIN2 is low level |
| 15 | 1 | DIN3 is high level (only valid when DIN3 is supported) |
|    | 0 | DIN3 is low level |

**Note:** DIN is the abbreviation of digital input port.
