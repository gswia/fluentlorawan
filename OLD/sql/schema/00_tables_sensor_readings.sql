-- sensor_readings hypertable
CREATE TABLE IF NOT EXISTS sensor_readings (
    timestamp_utc       TIMESTAMPTZ NOT NULL,
    account_id          VARCHAR NOT NULL,
    application_id      VARCHAR NOT NULL,
    site_id             VARCHAR NOT NULL,
    device_id           VARCHAR NOT NULL,
    sensor_id           VARCHAR NOT NULL,
    message_id          VARCHAR NOT NULL,
    type                VARCHAR NOT NULL,
    payload             JSONB,
    PRIMARY KEY (timestamp_utc, sensor_id)
);

-- Convert to hypertable
SELECT create_hypertable('sensor_readings', 'timestamp_utc', if_not_exists => TRUE);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_sensor_readings_account_time ON sensor_readings (account_id, timestamp_utc DESC);
CREATE INDEX IF NOT EXISTS idx_sensor_readings_device_time ON sensor_readings (device_id, timestamp_utc DESC);
CREATE INDEX IF NOT EXISTS idx_sensor_readings_sensor_time ON sensor_readings (sensor_id, timestamp_utc DESC);
CREATE INDEX IF NOT EXISTS idx_sensor_readings_type ON sensor_readings (type);
