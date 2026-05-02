-- =====================================================================
-- POPULATE v1 SCHEMA FROM EXISTING JSONB DATA
-- =====================================================================
-- This file contains INSERT statements extracted from the JSONB structure
-- in public.accounts table. Adjust timezone and names as needed.
-- =====================================================================

-- =====================================================================
-- STEP 1: INSERT ACCOUNT
-- =====================================================================
INSERT INTO v1.accounts (account_id, name, storage_config)
VALUES (
    '8f4a2e7c-5b1d-4f89-a3c6-9d8e7f6a5b4c'::UUID,
    'Sample Account',
    NULL
);

-- =====================================================================
-- STEP 2: INSERT GROUP (Site from JSONB)
-- =====================================================================
-- Site group with gateway (SiteId from JSONB becomes group_id)
INSERT INTO v1.groups (group_id, account_id, name, group_type, timezone, gateway_ids)
VALUES (
    '3c8f9a2e-7d4b-4e1f-9a5c-2b8e7f6a9d3c'::UUID,
    '8f4a2e7c-5b1d-4f89-a3c6-9d8e7f6a5b4c'::UUID,
    'Sample Site',
    'Site',
    'America/Phoenix',
    ARRAY['2cf7f11173000049']
);

-- =====================================================================
-- STEP 3: INSERT DEVICES
-- =====================================================================
INSERT INTO v1.devices (device_id, device_profile) VALUES
('a8404155476006c8', '{"type": "LHT52"}'::JSONB),
('a8404122c96006e4', '{"type": "LHT52"}'::JSONB),
('a840411d136006d7', '{"type": "LHT52"}'::JSONB),
('a840418b4b6006cd', '{"type": "LHT52"}'::JSONB),
('a84041224a600684', '{"type": "LHT52"}'::JSONB),
('a8404101da5f3677', '{"type": "LHT65N"}'::JSONB),
('a84041c10b5ff491', '{"type": "LHT65N"}'::JSONB),
('a8404136995ea57b', '{"type": "LDS02"}'::JSONB),
('a8404178215ea57c', '{"type": "LDS02"}'::JSONB),
('a8404134095f3c52', '{"type": "LHT65NVIB"}'::JSONB);

-- =====================================================================
-- STEP 4: INSERT SENSORS
-- =====================================================================
-- Device: a8404155476006c8 (Bedroom 2 - LHT52)
INSERT INTO v1.sensors (sensor_id, device_id, sensor_type, sensor_profile) VALUES
('1a2b3c4d-5e6f-4a7b-8c9d-0e1f2a3b4c5d'::UUID, 'a8404155476006c8', 'Temperature', '{"description": "Bedroom 2 temperature"}'::JSONB),
('2b3c4d5e-6f7a-4b8c-9d0e-1f2a3b4c5d6e'::UUID, 'a8404155476006c8', 'Humidity', '{"description": "Bedroom 2 humidity"}'::JSONB),
('a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d'::UUID, 'a8404155476006c8', 'Voltage', '{"description": "Bedroom 2 battery voltage"}'::JSONB),
('b2c3d4e5-f6a7-4b8c-9d0e-1f2a3b4c5d6e'::UUID, 'a8404155476006c8', 'DeviceModel', '{"description": "Bedroom 2 device model"}'::JSONB),
('c3d4e5f6-a7b8-4c9d-0e1f-2a3b4c5d6e7f'::UUID, 'a8404155476006c8', 'Firmware', '{"description": "Bedroom 2 firmware version"}'::JSONB),
('d4e5f6a7-b8c9-4d0e-1f2a-3b4c5d6e7f8a'::UUID, 'a8404155476006c8', 'RadioConfig', '{"description": "Bedroom 2 LoRa config"}'::JSONB),
('e5f6a7b8-c9d0-4e1f-2a3b-4c5d6e7f8a9b'::UUID, 'a8404155476006c8', 'Gateway', '{"description": "Bedroom 2 gateway info"}'::JSONB);

-- Device: a8404122c96006e4 (Living room - LHT52)
INSERT INTO v1.sensors (sensor_id, device_id, sensor_type, sensor_profile) VALUES
('3c4d5e6f-7a8b-4c9d-0e1f-2a3b4c5d6e7f'::UUID, 'a8404122c96006e4', 'Temperature', '{"description": "Living room temperature"}'::JSONB),
('4d5e6f7a-8b9c-4d0e-1f2a-3b4c5d6e7f8a'::UUID, 'a8404122c96006e4', 'Humidity', '{"description": "Living room humidity"}'::JSONB),
('f6a7b8c9-d0e1-4f2a-3b4c-5d6e7f8a9b0c'::UUID, 'a8404122c96006e4', 'Voltage', '{"description": "Living room battery voltage"}'::JSONB),
('a7b8c9d0-e1f2-4a3b-4c5d-6e7f8a9b0c1d'::UUID, 'a8404122c96006e4', 'DeviceModel', '{"description": "Living room device model"}'::JSONB),
('b8c9d0e1-f2a3-4b4c-5d6e-7f8a9b0c1d2e'::UUID, 'a8404122c96006e4', 'Firmware', '{"description": "Living room firmware version"}'::JSONB),
('c9d0e1f2-a3b4-4c5d-6e7f-8a9b0c1d2e3f'::UUID, 'a8404122c96006e4', 'RadioConfig', '{"description": "Living room LoRa config"}'::JSONB),
('d0e1f2a3-b4c5-4d6e-7f8a-9b0c1d2e3f4a'::UUID, 'a8404122c96006e4', 'Gateway', '{"description": "Living room gateway info"}'::JSONB);

-- Device: a840411d136006d7 (Bedroom 4 - LHT52)
INSERT INTO v1.sensors (sensor_id, device_id, sensor_type, sensor_profile) VALUES
('5e6f7a8b-9c0d-4e1f-2a3b-4c5d6e7f8a9b'::UUID, 'a840411d136006d7', 'Temperature', '{"description": "Bedroom 4 temperature"}'::JSONB),
('6f7a8b9c-0d1e-4f2a-3b4c-5d6e7f8a9b0c'::UUID, 'a840411d136006d7', 'Humidity', '{"description": "Bedroom 4 humidity"}'::JSONB),
('e1f2a3b4-c5d6-4e7f-8a9b-0c1d2e3f4a5b'::UUID, 'a840411d136006d7', 'Voltage', '{"description": "Bedroom 4 battery voltage"}'::JSONB),
('f2a3b4c5-d6e7-4f8a-9b0c-1d2e3f4a5b6c'::UUID, 'a840411d136006d7', 'DeviceModel', '{"description": "Bedroom 4 device model"}'::JSONB),
('a3b4c5d6-e7f8-4a9b-0c1d-2e3f4a5b6c7d'::UUID, 'a840411d136006d7', 'Firmware', '{"description": "Bedroom 4 firmware version"}'::JSONB),
('b4c5d6e7-f8a9-4b0c-1d2e-3f4a5b6c7d8e'::UUID, 'a840411d136006d7', 'RadioConfig', '{"description": "Bedroom 4 LoRa config"}'::JSONB),
('c5d6e7f8-a9b0-4c1d-2e3f-4a5b6c7d8e9f'::UUID, 'a840411d136006d7', 'Gateway', '{"description": "Bedroom 4 gateway info"}'::JSONB);

-- Device: a840418b4b6006cd (Bedroom 3 - LHT52)
INSERT INTO v1.sensors (sensor_id, device_id, sensor_type, sensor_profile) VALUES
('7a8b9c0d-1e2f-4a3b-4c5d-6e7f8a9b0c1d'::UUID, 'a840418b4b6006cd', 'Temperature', '{"description": "Bedroom 3 temperature"}'::JSONB),
('8b9c0d1e-2f3a-4b4c-5d6e-7f8a9b0c1d2e'::UUID, 'a840418b4b6006cd', 'Humidity', '{"description": "Bedroom 3 humidity"}'::JSONB),
('d6e7f8a9-b0c1-4d2e-3f4a-5b6c7d8e9f0a'::UUID, 'a840418b4b6006cd', 'Voltage', '{"description": "Bedroom 3 battery voltage"}'::JSONB),
('e7f8a9b0-c1d2-4e3f-4a5b-6c7d8e9f0a1b'::UUID, 'a840418b4b6006cd', 'DeviceModel', '{"description": "Bedroom 3 device model"}'::JSONB),
('f8a9b0c1-d2e3-4f4a-5b6c-7d8e9f0a1b2c'::UUID, 'a840418b4b6006cd', 'Firmware', '{"description": "Bedroom 3 firmware version"}'::JSONB),
('a9b0c1d2-e3f4-4a5b-6c7d-8e9f0a1b2c3d'::UUID, 'a840418b4b6006cd', 'RadioConfig', '{"description": "Bedroom 3 LoRa config"}'::JSONB),
('b0c1d2e3-f4a5-4b6c-7d8e-9f0a1b2c3d4e'::UUID, 'a840418b4b6006cd', 'Gateway', '{"description": "Bedroom 3 gateway info"}'::JSONB);

-- Device: a84041224a600684 (Master bedroom - LHT52)
INSERT INTO v1.sensors (sensor_id, device_id, sensor_type, sensor_profile) VALUES
('9c0d1e2f-3a4b-4c5d-6e7f-8a9b0c1d2e3f'::UUID, 'a84041224a600684', 'Temperature', '{"description": "Master bedroom temperature"}'::JSONB),
('0d1e2f3a-4b5c-4d6e-7f8a-9b0c1d2e3f4a'::UUID, 'a84041224a600684', 'Humidity', '{"description": "Master bedroom humidity"}'::JSONB),
('c1d2e3f4-a5b6-4c7d-8e9f-0a1b2c3d4e5f'::UUID, 'a84041224a600684', 'Voltage', '{"description": "Master bedroom battery voltage"}'::JSONB),
('d2e3f4a5-b6c7-4d8e-9f0a-1b2c3d4e5f6a'::UUID, 'a84041224a600684', 'DeviceModel', '{"description": "Master bedroom device model"}'::JSONB),
('e3f4a5b6-c7d8-4e9f-0a1b-2c3d4e5f6a7b'::UUID, 'a84041224a600684', 'Firmware', '{"description": "Master bedroom firmware version"}'::JSONB),
('f4a5b6c7-d8e9-4f0a-1b2c-3d4e5f6a7b8c'::UUID, 'a84041224a600684', 'RadioConfig', '{"description": "Master bedroom LoRa config"}'::JSONB),
('a5b6c7d8-e9f0-4a1b-2c3d-4e5f6a7b8c9d'::UUID, 'a84041224a600684', 'Gateway', '{"description": "Master bedroom gateway info"}'::JSONB);

-- Device: a8404101da5f3677 (Garage - LHT65N)
INSERT INTO v1.sensors (sensor_id, device_id, sensor_type, sensor_profile) VALUES
('1f2e3d4c-5b6a-4798-8e9d-0c1f2e3d4c5b'::UUID, 'a8404101da5f3677', 'Temperature', '{"description": "Garage temperature"}'::JSONB),
('2e3d4c5b-6a79-48e9-d0c1-f2e3d4c5b6a7'::UUID, 'a8404101da5f3677', 'Humidity', '{"description": "Garage humidity"}'::JSONB),
('3d4c5b6a-798e-49d0-c1f2-e3d4c5b6a798'::UUID, 'a8404101da5f3677', 'Voltage', '{"description": "Garage battery voltage"}'::JSONB),
('4c5b6a79-8e9d-40c1-f2e3-d4c5b6a798e9'::UUID, 'a8404101da5f3677', 'DeviceModel', '{"description": "Garage device model"}'::JSONB),
('5b6a798e-9d0c-41f2-e3d4-c5b6a798e9d0'::UUID, 'a8404101da5f3677', 'Firmware', '{"description": "Garage firmware version"}'::JSONB),
('6a798e9d-0c1f-42e3-d4c5-b6a798e9d0c1'::UUID, 'a8404101da5f3677', 'RadioConfig', '{"description": "Garage LoRa config"}'::JSONB),
('798e9d0c-1f2e-43d4-c5b6-a798e9d0c1f2'::UUID, 'a8404101da5f3677', 'Gateway', '{"description": "Garage gateway info"}'::JSONB);

-- Device: a84041c10b5ff491 (Outdoor - LHT65N)
INSERT INTO v1.sensors (sensor_id, device_id, sensor_type, sensor_profile) VALUES
('8e9d0c1f-2e3d-44c5-b6a7-98e9d0c1f2e3'::UUID, 'a84041c10b5ff491', 'Temperature', '{"description": "Outdoor temperature"}'::JSONB),
('9d0c1f2e-3d4c-45b6-a798-e9d0c1f2e3d4'::UUID, 'a84041c10b5ff491', 'Humidity', '{"description": "Outdoor humidity"}'::JSONB),
('0c1f2e3d-4c5b-46a7-98e9-d0c1f2e3d4c5'::UUID, 'a84041c10b5ff491', 'Voltage', '{"description": "Outdoor battery voltage"}'::JSONB),
('1f2e3d4c-5b6a-4798-e9d0-c1f2e3d4c5b6'::UUID, 'a84041c10b5ff491', 'Illumination', '{"description": "Outdoor illumination"}'::JSONB),
('4e6ce77a-6945-4fc3-9eeb-108a1e06689a'::UUID, 'a84041c10b5ff491', 'DeviceModel', '{"description": "Outdoor device model"}'::JSONB),
('05b478b9-97c8-4965-8f4d-028231528221'::UUID, 'a84041c10b5ff491', 'Firmware', '{"description": "Outdoor firmware version"}'::JSONB),
('6e7ff32e-8640-4296-b78a-ba3987eb9ff8'::UUID, 'a84041c10b5ff491', 'RadioConfig', '{"description": "Outdoor LoRa config"}'::JSONB),
('486a85c5-5666-4ded-9b97-223cbeb61d13'::UUID, 'a84041c10b5ff491', 'Gateway', '{"description": "Outdoor gateway info"}'::JSONB);

-- Device: a8404136995ea57b (Front door - LDS02)
INSERT INTO v1.sensors (sensor_id, device_id, sensor_type, sensor_profile) VALUES
('a1b2c3d4-e5f6-47a8-9b0c-1d2e3f4a5b6c'::UUID, 'a8404136995ea57b', 'Door', '{"description": "Front door status"}'::JSONB),
('b2c3d4e5-f6a7-48b9-0c1d-2e3f4a5b6c7d'::UUID, 'a8404136995ea57b', 'Voltage', '{"description": "Front door battery voltage"}'::JSONB),
('f6a7b8c9-d0e1-42f3-4a5b-6c7d8e9f0a1b'::UUID, 'a8404136995ea57b', 'Gateway', '{"description": "Front door gateway info"}'::JSONB);

-- Device: a8404178215ea57c (Patio door - LDS02)
INSERT INTO v1.sensors (sensor_id, device_id, sensor_type, sensor_profile) VALUES
('1a2b3c4d-5e6f-47a8-9b0c-1d2e3f4a5b6c'::UUID, 'a8404178215ea57c', 'Door', '{"description": "Patio door status"}'::JSONB),
('2b3c4d5e-6f7a-48b9-0c1d-2e3f4a5b6c7d'::UUID, 'a8404178215ea57c', 'Voltage', '{"description": "Patio door battery voltage"}'::JSONB),
('6f7a8b9c-0d1e-42f3-4a5b-6c7d8e9f0a1b'::UUID, 'a8404178215ea57c', 'Gateway', '{"description": "Patio door gateway info"}'::JSONB);

-- Device: a8404134095f3c52 (HVAC - LHT65NVIB)
INSERT INTO v1.sensors (sensor_id, device_id, sensor_type, sensor_profile) VALUES
('7a8b9c0d-1e2f-43a4-5b6c-7d8e9f0a1b2c'::UUID, 'a8404134095f3c52', 'Vibration', '{"description": "HVAC vibration monitoring"}'::JSONB),
('8b9c0d1e-2f3a-44b5-6c7d-8e9f0a1b2c3d'::UUID, 'a8404134095f3c52', 'Temperature', '{"description": "HVAC temperature"}'::JSONB),
('9c0d1e2f-3a4b-45c6-7d8e-9f0a1b2c3d4e'::UUID, 'a8404134095f3c52', 'Humidity', '{"description": "HVAC humidity"}'::JSONB),
('0d1e2f3a-4b5c-46d7-8e9f-0a1b2c3d4e5f'::UUID, 'a8404134095f3c52', 'Acceleration', '{"description": "HVAC max acceleration"}'::JSONB),
('1e2f3a4b-5c6d-47e8-9f0a-1b2c3d4e5f6a'::UUID, 'a8404134095f3c52', 'Voltage', '{"description": "HVAC battery voltage"}'::JSONB),
('2f3a4b5c-6d7e-48f9-0a1b-2c3d4e5f6a7b'::UUID, 'a8404134095f3c52', 'DeviceModel', '{"description": "HVAC device model"}'::JSONB),
('3a4b5c6d-7e8f-490a-1b2c-3d4e5f6a7b8c'::UUID, 'a8404134095f3c52', 'Firmware', '{"description": "HVAC firmware version"}'::JSONB),
('4b5c6d7e-8f9a-400b-2c3d-4e5f6a7b8c9d'::UUID, 'a8404134095f3c52', 'RadioConfig', '{"description": "HVAC LoRa config"}'::JSONB),
('5c6d7e8f-9a0b-411c-3d4e-5f6a7b8c9d0e'::UUID, 'a8404134095f3c52', 'Gateway', '{"description": "HVAC gateway info"}'::JSONB);

-- =====================================================================
-- STEP 5: INSERT DEVICE-GROUP ASSOCIATIONS
-- =====================================================================
-- Associate all devices with the Site group
INSERT INTO v1.device_groups (device_id, group_id) VALUES
('a8404155476006c8', '3c8f9a2e-7d4b-4e1f-9a5c-2b8e7f6a9d3c'::UUID),
('a8404122c96006e4', '3c8f9a2e-7d4b-4e1f-9a5c-2b8e7f6a9d3c'::UUID),
('a840411d136006d7', '3c8f9a2e-7d4b-4e1f-9a5c-2b8e7f6a9d3c'::UUID),
('a840418b4b6006cd', '3c8f9a2e-7d4b-4e1f-9a5c-2b8e7f6a9d3c'::UUID),
('a84041224a600684', '3c8f9a2e-7d4b-4e1f-9a5c-2b8e7f6a9d3c'::UUID),
('a8404101da5f3677', '3c8f9a2e-7d4b-4e1f-9a5c-2b8e7f6a9d3c'::UUID),
('a84041c10b5ff491', '3c8f9a2e-7d4b-4e1f-9a5c-2b8e7f6a9d3c'::UUID),
('a8404136995ea57b', '3c8f9a2e-7d4b-4e1f-9a5c-2b8e7f6a9d3c'::UUID),
('a8404178215ea57c', '3c8f9a2e-7d4b-4e1f-9a5c-2b8e7f6a9d3c'::UUID),
('a8404134095f3c52', '3c8f9a2e-7d4b-4e1f-9a5c-2b8e7f6a9d3c'::UUID);

-- =====================================================================
-- VERIFICATION QUERIES
-- =====================================================================
-- Uncomment these to verify the data was inserted correctly:

-- SELECT * FROM v1.accounts;
-- SELECT * FROM v1.groups ORDER BY group_type;
-- SELECT device_id, device_profile->>'type' AS type, device_profile->>'location' AS location FROM v1.devices ORDER BY device_id;
-- SELECT device_id, sensor_type, COUNT(*) FROM v1.sensors GROUP BY device_id, sensor_type ORDER BY device_id, sensor_type;
-- SELECT COUNT(*) AS total_sensors FROM v1.sensors;
-- SELECT g.name, COUNT(dg.device_id) AS device_count FROM v1.groups g LEFT JOIN v1.device_groups dg ON dg.group_id = g.group_id GROUP BY g.group_id, g.name;
