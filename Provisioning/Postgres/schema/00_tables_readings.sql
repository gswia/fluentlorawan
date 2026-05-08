-- sensor_readings hypertable
-- Replaces OLD application_id/site_id with group_id for Group-based model
CREATE TABLE IF NOT EXISTS v1.sensor_readings (
    timestamp_utc       TIMESTAMPTZ NOT NULL,
    account_id          UUID NOT NULL,
    group_id            UUID NOT NULL,
    device_id           TEXT NOT NULL,  -- DevEUI
    sensor_id           UUID NOT NULL,
    message_id          UUID NOT NULL,
    type                TEXT NOT NULL,
    payload             JSONB,
    PRIMARY KEY (timestamp_utc, sensor_id)
);

-- Convert to hypertable
SELECT create_hypertable('v1.sensor_readings', 'timestamp_utc', if_not_exists => TRUE);

-- Indexes for sensor_readings
CREATE INDEX IF NOT EXISTS sensor_readings_timestamp_utc_idx 
    ON v1.sensor_readings (timestamp_utc DESC);
    
CREATE INDEX IF NOT EXISTS idx_sensor_readings_type 
    ON v1.sensor_readings (type);
    
CREATE INDEX IF NOT EXISTS idx_sensor_readings_type_sensor_time 
    ON v1.sensor_readings (type, sensor_id, timestamp_utc DESC);
    
CREATE INDEX IF NOT EXISTS idx_sensor_readings_sensor_time 
    ON v1.sensor_readings (sensor_id, timestamp_utc DESC);
    
CREATE INDEX IF NOT EXISTS idx_sensor_readings_device_time 
    ON v1.sensor_readings (device_id, timestamp_utc DESC);
    
CREATE INDEX IF NOT EXISTS idx_sensor_readings_account_time 
    ON v1.sensor_readings (account_id, timestamp_utc DESC);


-- gateway_readings hypertable
CREATE TABLE IF NOT EXISTS v1.gateway_readings (
    timestamp_utc       TIMESTAMPTZ NOT NULL,
    account_id          UUID NOT NULL,
    group_id            UUID NOT NULL,
    device_id           TEXT NOT NULL,  -- DevEUI
    gateway_id          TEXT NOT NULL,  -- Gateway EUI
    message_id          UUID NOT NULL,
    type                TEXT NOT NULL,
    payload             JSONB
);

-- Convert to hypertable
SELECT create_hypertable('v1.gateway_readings', 'timestamp_utc', if_not_exists => TRUE);

-- Indexes for gateway_readings
CREATE INDEX IF NOT EXISTS gateway_readings_timestamp_utc_idx 
    ON v1.gateway_readings (timestamp_utc DESC);
    
CREATE INDEX IF NOT EXISTS idx_gateway_readings_type 
    ON v1.gateway_readings (type);
    
CREATE INDEX IF NOT EXISTS idx_gateway_readings_account_time 
    ON v1.gateway_readings (account_id, timestamp_utc DESC);
    
CREATE INDEX IF NOT EXISTS idx_gateway_readings_device_time 
    ON v1.gateway_readings (device_id, timestamp_utc DESC);
    
CREATE INDEX IF NOT EXISTS idx_gateway_readings_gateway_time 
    ON v1.gateway_readings (gateway_id, timestamp_utc DESC);
