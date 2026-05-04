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
