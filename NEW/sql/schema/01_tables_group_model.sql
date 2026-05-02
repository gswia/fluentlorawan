-- Group Model Tables
-- Replaces OLD Application->Site hierarchy with flexible Group-based model

-- accounts table
CREATE TABLE IF NOT EXISTS v1.accounts (
    account_id          UUID PRIMARY KEY,
    name                TEXT NOT NULL,
    storage_config      JSONB
);

-- users table
CREATE TABLE IF NOT EXISTS v1.users (
    user_id             UUID PRIMARY KEY,
    email               TEXT NOT NULL UNIQUE,
    name                TEXT NOT NULL
);

-- roles table
CREATE TABLE IF NOT EXISTS v1.roles (
    role_id             UUID PRIMARY KEY,
    name                TEXT NOT NULL,
    permissions         TEXT[]
);

-- user_accounts junction table
CREATE TABLE IF NOT EXISTS v1.user_accounts (
    user_id             UUID NOT NULL,
    account_id          UUID NOT NULL,
    role_id             UUID NOT NULL,
    PRIMARY KEY (user_id, account_id, role_id)
);

-- groups table (replaces OLD sites table)
CREATE TABLE IF NOT EXISTS v1.groups (
    group_id            UUID PRIMARY KEY,
    account_id          UUID NOT NULL,
    name                TEXT NOT NULL,
    group_type          TEXT NOT NULL,
    timezone            TEXT NOT NULL
);

-- CRITICAL index for window function filtering (mirrors OLD sites table pattern)
CREATE INDEX IF NOT EXISTS idx_groups_timezone ON v1.groups (timezone);

-- devices table
CREATE TABLE IF NOT EXISTS v1.devices (
    device_id           UUID PRIMARY KEY,
    device_profile      JSONB
);

-- sensors table
CREATE TABLE IF NOT EXISTS v1.sensors (
    sensor_id           UUID PRIMARY KEY,
    device_id           UUID NOT NULL,
    sensor_type         TEXT NOT NULL,
    description         TEXT
);

-- Index on device_id (auto-created from FK, needed for device_groups joins)
CREATE INDEX IF NOT EXISTS idx_sensors_device_id ON v1.sensors (device_id);

-- device_groups junction table (replaces OLD sensor_site_map)
-- Supports many-to-many: devices can belong to multiple groups
CREATE TABLE IF NOT EXISTS v1.device_groups (
    device_id           UUID NOT NULL,
    group_id            UUID NOT NULL,
    PRIMARY KEY (device_id, group_id)
);

-- CRITICAL index for "group → devices" reverse lookups
-- Mirrors OLD sensor_site_map pattern: WHERE site_id IN (...)
CREATE INDEX IF NOT EXISTS idx_device_groups_group_id ON v1.device_groups (group_id);
