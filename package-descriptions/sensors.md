# Sensors

Sensor is a compound data type. It contains all data related to sensors, e.g. temperature, humidity, etc.

## Structure

| Name | Bytes | Description |
|---|---|---|
| Temperature | 2 | Internal temperature (in (1/256)°C) — Signed 16 bits integer |
| Humidity | 2 | Humidity (in (1/10)%) — Unsigned 16 bits integer |
| Illuminance | 4 | Illuminance (in (1/256)lx) — Unsigned 32 bits integer |
| CO2 | 4 | CO2 concentration (in ppm) — Unsigned 32 bits integer |
| Probe | 2 | Probe temperature (in (1/16)°C) — Signed 16 bits integer |
