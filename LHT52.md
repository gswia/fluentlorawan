# LHT52 Sensor-to-Reading Mapping

| Sensor Type | Reading Type Class | ChirpStack Field | fPort(s) | Value Type | Unit | Purpose |
|-------------|-------------------|------------------|----------|------------|------|---------|
| `Temperature` | `Temperature` | `TempC_SHT` | 2, 3 | Scalar | °C | Ambient temperature |
| `Humidity` | `Humidity` | `Hum_SHT` | 2, 3 | Scalar | %RH | Ambient humidity |
| `TemperatureProbe` | `Temperature` | `TempC_DS` | 2, 3 | Scalar | °C | External probe temp |
| `TemperatureProbe` | `ProbeType` | `Ext` | 2, 3 | Scalar | number | Which probe attached |
| `Voltage` | `Voltage` | `Bat_mV` | 5 | Scalar | mV | Power level |
| `TemperatureProbe` | `ProbeId` | `DS18B20_ID` | 4 | Scalar | hex string | Probe serial number |
| `DeviceModel` | `ModelNumber` | `Sensor_Model` | 5 | Scalar | number | Hardware variant |
| `Firmware` | `Version` | `Firmware_Version` | 5 | Scalar | string | Software version |
| `RadioConfig` | `LoRaConfig` | `Freq_Band`, `Sub_Band` | 5 | Composite | number, number | Radio configuration |

## Additional Outputs

- `Node_type` = "LHT52" (all fPorts) - device identifier, not a sensor
- `Status` = "RPL data or sensor reset" (fPort 2, if bytes.length != 11) - error condition
- `DATALOG` = concatenated string (fPort 3) - batch of historical readings from sensors 1-4
- `Systimestamp` (fPort 2, 3) - becomes `Reading.Timestamp` property, not a separate sensor

**9 sensor types total.**
