# Graph Data - Mesa Airbnb

## Subjects (Nodes)
```json
[
  {id: "prop_001", name: "Property", type: "property", metadata: {propertyType: "Airbnb", city: "Mesa", state: "Arizona", sqft: 1681, timezone: "America/Phoenix"}},
  {id: "master", name: "Master Bedroom", type: "room", subtype: "bedroom", metadata: {windows: "north", hvacNotes: "shortest cooling path from HVAC"}},
  {id: "br2", name: "Bedroom 2", type: "room", subtype: "bedroom", metadata: {windows: "east"}},
  {id: "br3", name: "Bedroom 3", type: "room", subtype: "bedroom", metadata: {windows: "east"}},
  {id: "br4", name: "Bedroom 4", type: "room", subtype: "bedroom", metadata: {windows: "west, south"}},
  {id: "hallway", name: "Hallway", type: "room", subtype: "common", metadata: {}},
  {id: "living", name: "Living Room", type: "room", subtype: "common", metadata: {}},
  {id: "kitchen", name: "Kitchen", type: "room", subtype: "common", metadata: {}},
  {id: "garage", name: "Garage", type: "room", subtype: "garage", metadata: {hvac: "separate mini-split at 80°F"}},
  {id: "backyard", name: "Backyard Patio", type: "outdoor", metadata: {features: "pool"}},
  {id: "frontdoor", name: "Front Door", type: "entry", subtype: "door", metadata: {faces: "south"}},
  {id: "patiodoor", name: "Patio Door", type: "entry", subtype: "door", metadata: {access: "backyard"}},
  {id: "kitchendoor", name: "Kitchen-Garage Door", type: "entry", subtype: "door", metadata: {}},
  {id: "garagedoor", name: "Garage Door", type: "entry", subtype: "door", metadata: {faces: "south"}},
  {id: "hvac", name: "York HVAC", type: "equipment", subtype: "hvac", metadata: {brand: "York", location: "roof above master bedroom", ductInterpretation: "sequential branches reduce airflow"}},
  {id: "thermostat", name: "Ecobee Thermostat", type: "equipment", subtype: "thermostat", metadata: {brand: "Ecobee", location: "hallway", hasRemoteSensors: false}}
]
```

## Sensors (Nodes)
```json
[
  {id: "s_master_temp", deviceId: "temp_001", sensorType: "Temperature"},
  {id: "s_master_hum", deviceId: "temp_001", sensorType: "Humidity"},
  {id: "s_br2_temp", deviceId: "temp_002", sensorType: "Temperature"},
  {id: "s_br2_hum", deviceId: "temp_002", sensorType: "Humidity"},
  {id: "s_br3_temp", deviceId: "temp_003", sensorType: "Temperature"},
  {id: "s_br3_hum", deviceId: "temp_003", sensorType: "Humidity"},
  {id: "s_br4_temp", deviceId: "temp_004", sensorType: "Temperature"},
  {id: "s_br4_hum", deviceId: "temp_004", sensorType: "Humidity"},
  {id: "s_br4_volt", deviceId: "temp_004", sensorType: "Voltage"},
  {id: "s_backyard_temp", deviceId: "outdoor_001", sensorType: "Temperature"},
  {id: "s_backyard_hum", deviceId: "outdoor_001", sensorType: "Humidity"},
  {id: "s_backyard_lux", deviceId: "outdoor_001", sensorType: "Illumination", metadata: {location: "under covered patio", shaded: true, notes: "prevents lux overflow"}},
  {id: "s_hvac_vib", deviceId: "vib_001", sensorType: "Vibration", metadata: {notes: "detects on/off, NOT abnormal vibration"}},
  {id: "s_hvac_volt", deviceId: "vib_001", sensorType: "Voltage"},
  {id: "s_frontdoor", deviceId: "door_001", sensorType: "Door"},
  {id: "s_patiodoor", deviceId: "door_002", sensorType: "Door"},
  {id: "s_kitchendoor", deviceId: "door_003", sensorType: "Door"},
  {id: "s_garagedoor", deviceId: "door_004", sensorType: "Door"}
]
```

## Relationships (Edges)
```json
[
  {from: "prop_001", to: "master", type: "CONTAINS"},
  {from: "prop_001", to: "br2", type: "CONTAINS"},
  {from: "prop_001", to: "br3", type: "CONTAINS"},
  {from: "prop_001", to: "br4", type: "CONTAINS"},
  {from: "prop_001", to: "hallway", type: "CONTAINS"},
  {from: "prop_001", to: "living", type: "CONTAINS"},
  {from: "prop_001", to: "kitchen", type: "CONTAINS"},
  {from: "prop_001", to: "garage", type: "CONTAINS"},
  {from: "prop_001", to: "backyard", type: "CONTAINS"},
  {from: "prop_001", to: "frontdoor", type: "CONTAINS"},
  {from: "prop_001", to: "patiodoor", type: "CONTAINS"},
  {from: "prop_001", to: "kitchendoor", type: "CONTAINS"},
  {from: "prop_001", to: "garagedoor", type: "CONTAINS"},
  {from: "prop_001", to: "hvac", type: "CONTAINS"},
  {from: "prop_001", to: "thermostat", type: "CONTAINS"},
  
  {from: "master", to: "s_master_temp", type: "CONTAINS_SENSOR"},
  {from: "master", to: "s_master_hum", type: "CONTAINS_SENSOR"},
  {from: "br2", to: "s_br2_temp", type: "CONTAINS_SENSOR"},
  {from: "br2", to: "s_br2_hum", type: "CONTAINS_SENSOR"},
  {from: "br3", to: "s_br3_temp", type: "CONTAINS_SENSOR"},
  {from: "br3", to: "s_br3_hum", type: "CONTAINS_SENSOR"},
  {from: "br4", to: "s_br4_temp", type: "CONTAINS_SENSOR"},
  {from: "br4", to: "s_br4_hum", type: "CONTAINS_SENSOR"},
  {from: "br4", to: "s_br4_volt", type: "CONTAINS_SENSOR"},
  {from: "backyard", to: "s_backyard_temp", type: "CONTAINS_SENSOR"},
  {from: "backyard", to: "s_backyard_hum", type: "CONTAINS_SENSOR"},
  {from: "backyard", to: "s_backya_temp", type: "HAS_SENSOR"},
  {from: "prop_001", to: "s_master_hum", type: "HAS_SENSOR"},
  {from: "prop_001", to: "s_br2_temp", type: "HAS_SENSOR"},
  {from: "prop_001", to: "s_br2_hum", type: "HAS_SENSOR"},
  {from: "prop_001", to: "s_br3_temp", type: "HAS_SENSOR"},
  {from: "prop_001", to: "s_br3_hum", type: "HAS_SENSOR"},
  {from: "prop_001", to: "s_br4_temp", type: "HAS_SENSOR"},
  {from: "prop_001", to: "s_br4_hum", type: "HAS_SENSOR"},
  {from: "prop_001", to: "s_br4_volt", type: "HAS_SENSOR"},
  {from: "prop_001", to: "s_backyard_temp", type: "HAS_SENSOR"},
  {from: "prop_001", to: "s_backyard_hum", type: "HAS_SENSOR"},
  {from: "prop_001", to: "s_backyard_lux", type: "HAS_SENSOR"},
  {from: "prop_001", to: "s_hvac_vib", type: "HAS_SENSOR"},
  {from: "prop_001", to: "s_hvac_voltitchendoor", type: "CONTAINS_SENSOR"},
  {from: "garagedoor", to: "s_garagedoor", type: "CONTAINS_SENSOR"},
  
  {from: "prop_001", to: "s_master", type: "HAS_SENSOR"},
  {from: "prop_001", to: "s_br2", type: "HAS_SENSOR"},
  {from: "prop_001", to: "s_br3", type: "HAS_SENSOR"},
  {from: "prop_001", to: "s_br4", type: "HAS_SENSOR"},
  {from: "prop_001", to: "s_backyard", type: "HAS_SENSOR"},
  {from: "prop_001", to: "s_hvac", type: "HAS_SENSOR"},
  {from: "prop_001", to: "s_frontdoor", type: "HAS_SENSOR"},
  {from: "prop_001", to: "s_patiodoor", type: "HAS_SENSOR"},
  {from: "prop_001", to: "s_kitchendoor", type: "HAS_SENSOR"},
  {from: "prop_001", to: "s_garagedoor", type: "HAS_SENSOR"},
  
  {from: "hvac", to: "master", type: "DUCT_BRANCH", metadata: {position: 1}},
  {from: "master", to: "br2", type: "DUCT_CONTINUES", metadata: {position: 2}},
  {from: "br2", to: "br3", type: "DUCT_CONTINUES", metadata: {position: 3}},
  {from: "br3", to: "br4", type: "DUCT_CONTINUES", metadata: {position: 4}}
]
```

## SensorTypes (Universal)
```json
[
  {name: "Temperature", unit: "celsius", displayUnit: "fahrenheit"},
  {name: "Humidity", unit: "percent", displayUnit: "percent"},
  {name: "Door", unit: "events", displayUnit: "open/closed"},
  {name: "Illumination", unit: "lux", displayUnit: "lux"},
  {name: "Vibration", unit: "binary", displayUnit: "on/off", notes: "HVAC cycles"},
  {name: "Acceleration", unit: "binary", displayUnit: "stable/tilt", notes: "stability, NOT vibration"},
  {name: "Voltage", unit: "volts", displayUnit: "battery"}
]
```

## Intents (Universal)
```json
[
  {
    name: "TEMPERATURE_CURRENT",
    keywords: ["temp", "temperature", "warm", "hot", "cold", "cool"],
    sensorTypes: ["Temperature"],
    contextKeys: ["windows", "sunExposure", "hvacNotes"],
    relevantRelationships: ["DUCT_BRANCH", "DUCT_CONTINUES"],
    tool: "get_sensor_analysis",
    needsPreviousWindow: false,
    defaultWindow: "1 hours",
    requiresLocation: false
  },
  {
    name: "TEMPERATURE_COMPARISON",
    keywords: ["warmer than", "cooler than", "vs yesterday"],
    sensorTypes: ["Temperature"],
    contextKeys: ["windows", "sunExposure"],
    relevantRelationships: ["DUCT_BRANCH", "DUCT_CONTINUES"],
    tool: "get_sensor_analysis",
    needsPreviousWindow: true,
    defaultWindow: "24 hours",
    defaultPreviousWindow: "24 hours"
  },
  {
    name: "HUMIDITY_CURRENT",
    keywords: ["humidity", "humid", "damp", "dry"],
    sensorTypes: ["Humidity"],
    contextKeys: [],
    relevantRelationships: [],
    tool: "get_sensor_analysis",
    needsPreviousWindow: false,
    defaultWindow: "1 hours"
  },
  {
    name: "DOOR_STATUS",
    keywords: ["door", "open", "closed", "activity"],
    sensorTypes: ["Door"],
    contextKeys: ["faces", "access"],
    relevantRelationships: [],
    tool: "get_sensor_analysis",
    needsPreviousWindow: false,
    defaultWindow: "24 hours",
    requiresLocation: true
  },
  {
    name: "ILLUMINATION_CURRENT",
    keywords: ["bright", "light", "lux", "dark"],
    sensorTypes: ["Illumination"],
    contextKeys: ["shaded", "covered"],
    relevantRelationships: [],
    tool: "get_sensor_analysis",
    needsPreviousWindow: false,
    defaultWindow: "1 hours"
  },
  {
    name: "VIBRATION_RUNTIME",
    keywords: ["hvac", "ac", "a/c", "cooling", "air conditioning"],
    sensorTypes: ["Vibration"],
    contextKeys: ["brand", "location"],
    relevantRelationships: [],
    tool: "get_sensor_analysis",
    needsPreviousWindow: false,
    defaultWindow: "24 hours"
  },
  {
    name: "ACCELERATION_STATUS",
    keywords: ["stable", "tilt", "stability"],
    sensorTypes: ["Acceleration"],
    contextKeys: [],
    relevantRelationships: [],
    tool: "get_sensor_analysis",
    needsPreviousWindow: false,
    defaultWindow: "1 hours"
  },
  {
    name: "BATTERY_STATUS",
    keywords: ["battery", "batteries", "low battery", "voltage"],
    sensorTypes: ["Voltage"],
    contextKeys: [],
    relevantRelationships: [],
    traversalPattern: "flat",
    tool: "get_sensor_analysis",
    needsPreviousWindow: false,
    defaultWindow: "1 hours"
  },
  {
    name: "HOUSE_OVERVIEW",
    keywords: ["house", "everything", "status", "overview"],
    sensorTypes: ["all"],
    contextKeys: [],
    relevantRelationships: [],
    traversalPattern: "flat",
    tool: "get_sensor_analysis",
    needsPreviousWindow: false,
    defaultWindow: "24 hours"
  }
]
```
