-- accounts table (contains JSONB hierarchy: Applications->Sites->Devices->Sensors)
CREATE TABLE IF NOT EXISTS accounts (
    account_id          VARCHAR PRIMARY KEY,
    account             JSONB
);

-- sensor_site_map lookup table
CREATE TABLE IF NOT EXISTS sensor_site_map (
    sensor_id           VARCHAR NOT NULL,
    device_id           VARCHAR NOT NULL,
    site_id             VARCHAR NOT NULL,
    account_id          VARCHAR NOT NULL
);

-- sites table (timezone info)
CREATE TABLE IF NOT EXISTS sites (
    site_id             VARCHAR PRIMARY KEY,
    timezone            VARCHAR NOT NULL
);
