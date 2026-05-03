-- Single insert functions for v1 schema
-- Individual inserts to avoid batch failures affecting all readings

-- Get device configuration (returns NULL columns if device not found)
CREATE OR REPLACE FUNCTION v1.get_device_config(p_device_id TEXT)
RETURNS TABLE (
    group_id UUID,
    account_id UUID,
    sensor_id UUID,
    sensor_type TEXT
)
LANGUAGE sql STABLE AS $$
    SELECT 
        dg.group_id,
        g.account_id,
        s.sensor_id,
        s.sensor_type
    FROM v1.devices d
    JOIN v1.device_groups dg ON dg.device_id = d.device_id
    JOIN v1.groups g ON g.group_id = dg.group_id
    LEFT JOIN v1.sensors s ON s.device_id = d.device_id
    WHERE d.device_id = p_device_id;
$$;

-- Insert single sensor reading
CREATE OR REPLACE FUNCTION v1.insert_sensor_reading(
    p_timestamp_utc TIMESTAMPTZ,
    p_account_id UUID,
    p_group_id UUID,
    p_device_id TEXT,
    p_sensor_id UUID,
    p_message_id UUID,
    p_type TEXT,
    p_payload JSONB
)
RETURNS VOID
LANGUAGE sql AS $$
    INSERT INTO v1.sensor_readings 
        (timestamp_utc, account_id, group_id, device_id, sensor_id, message_id, type, payload)
    VALUES 
        (p_timestamp_utc, p_account_id, p_group_id, p_device_id, p_sensor_id, p_message_id, p_type, p_payload);
$$;

-- Insert single gateway reading
CREATE OR REPLACE FUNCTION v1.insert_gateway_reading(
    p_timestamp_utc TIMESTAMPTZ,
    p_account_id UUID,
    p_group_id UUID,
    p_device_id TEXT,
    p_gateway_id TEXT,
    p_message_id UUID,
    p_type TEXT,
    p_payload JSONB
)
RETURNS VOID
LANGUAGE sql AS $$
    INSERT INTO v1.gateway_readings 
        (timestamp_utc, account_id, group_id, device_id, gateway_id, message_id, type, payload)
    VALUES 
        (p_timestamp_utc, p_account_id, p_group_id, p_device_id, p_gateway_id, p_message_id, p_type, p_payload);
$$;

-- Example usage:
/*
-- Get device config once
SELECT * FROM v1.get_device_config('a8404155476006c8');

-- Insert sensor reading
SELECT v1.insert_sensor_reading(
    '2026-05-03T10:30:00Z'::timestamptz,
    '8f4a2e7c-5b1d-4f89-a3c6-9d8e7f6a5b4c'::uuid,
    '3c8f9a2e-7d4b-4e1f-9a5c-2b8e7f6a9d3c'::uuid,
    'a8404155476006c8',
    '12345678-1234-1234-1234-123456789abc'::uuid,
    '87654321-4321-4321-4321-210987654321'::uuid,
    'Temperature',
    '{"ValueC": 22.5}'::jsonb
);

-- Insert gateway reading
SELECT v1.insert_gateway_reading(
    '2026-05-03T10:30:00Z'::timestamptz,
    '8f4a2e7c-5b1d-4f89-a3c6-9d8e7f6a5b4c'::uuid,
    '3c8f9a2e-7d4b-4e1f-9a5c-2b8e7f6a9d3c'::uuid,
    'a8404155476006c8',
    'b827ebfffed8c123',
    '87654321-4321-4321-4321-210987654321'::uuid,
    'Gateway',
    '{"Rssi": -80, "Snr": 7.5}'::jsonb
);
*/
