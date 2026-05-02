-- sensor_readings hypertable
-- Replaces OLD application_id/site_id with group_id for Group-based model
CREATE TABLE IF NOT EXISTS v1.sensor_readings (
    timestamp_utc       TIMESTAMPTZ NOT NULL,
    account_id          UUID NOT NULL,
    group_id            UUID NOT NULL,
    device_id           UUID NOT NULL,
    sensor_id           UUID NOT NULL,
    message_id          UUID NOT NULL,
    type                TEXT NOT NULL,
    payload             JSONB,
    PRIMARY KEY (timestamp_utc, sensor_id)
);

-- Convert to hypertable
SELECT create_hypertable('v1.sensor_readings', 'timestamp_utc', if_not_exists => TRUE);

-- CRITICAL index for continuous aggregate WHERE filters
CREATE INDEX IF NOT EXISTS idx_sensor_readings_type ON v1.sensor_readings (type);


-- gateway_readings hypertable
CREATE TABLE IF NOT EXISTS v1.gateway_readings (
    timestamp_utc       TIMESTAMPTZ NOT NULL,
    account_id          UUID NOT NULL,
    group_id            UUID NOT NULL,
    device_id           UUID NOT NULL,
    gateway_id          UUID NOT NULL,
    message_id          UUID NOT NULL,
    type                TEXT NOT NULL,
    payload             JSONB
);

-- Convert to hypertable
SELECT create_hypertable('v1.gateway_readings', 'timestamp_utc', if_not_exists => TRUE);

-- CRITICAL index for continuous aggregate WHERE filters (if using type-based caggs)
CREATE INDEX IF NOT EXISTS idx_gateway_readings_type ON v1.gateway_readings (type);
