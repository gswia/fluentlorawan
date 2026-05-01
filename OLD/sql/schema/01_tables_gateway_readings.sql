-- gateway_readings hypertable
CREATE TABLE IF NOT EXISTS gateway_readings (
    timestamp_utc       TIMESTAMPTZ NOT NULL,
    account_id          VARCHAR NOT NULL,
    application_id      VARCHAR NOT NULL,
    site_id             VARCHAR NOT NULL,
    device_id           VARCHAR NOT NULL,
    gateway_id          VARCHAR NOT NULL,
    message_id          VARCHAR NOT NULL,
    type                VARCHAR NOT NULL,
    payload             JSONB
);

-- Convert to hypertable
SELECT create_hypertable('gateway_readings', 'timestamp_utc', if_not_exists => TRUE);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_gateway_readings_account_time ON gateway_readings (account_id, timestamp_utc DESC);
CREATE INDEX IF NOT EXISTS idx_gateway_readings_device_time ON gateway_readings (device_id, timestamp_utc DESC);
CREATE INDEX IF NOT EXISTS idx_gateway_readings_gateway_time ON gateway_readings (gateway_id, timestamp_utc DESC);
