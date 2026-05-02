-- =====================================================================
-- Complete v1 Schema Deployment Script
-- Deploy v1 schema for Group-based IoT model to TimescaleDB
-- Isolates new schema from existing public schema
-- =====================================================================

-- =====================================================================
-- PHASE 1: DROP AND CREATE SCHEMA NAMESPACE
-- =====================================================================
DROP SCHEMA IF EXISTS v1 CASCADE;
CREATE SCHEMA v1;

-- =====================================================================
-- PHASE 2: CREATE HYPERTABLES
-- =====================================================================

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

-- =====================================================================
-- PHASE 3: CREATE GROUP MODEL TABLES
-- =====================================================================

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
    timezone            TEXT NOT NULL,
    gateway_ids         TEXT[]  -- LoRaWAN gateway EUIs
);

-- CRITICAL index for window function filtering (mirrors OLD sites table pattern)
CREATE INDEX IF NOT EXISTS idx_groups_timezone ON v1.groups (timezone);

-- devices table
CREATE TABLE IF NOT EXISTS v1.devices (
    device_id           TEXT PRIMARY KEY,  -- DevEUI (LoRaWAN device identifier)
    device_profile      JSONB
);

-- sensors table
CREATE TABLE IF NOT EXISTS v1.sensors (
    sensor_id           UUID PRIMARY KEY,
    device_id           TEXT NOT NULL,  -- DevEUI reference
    sensor_type         TEXT NOT NULL,
    sensor_profile      JSONB
);

-- Index on device_id (auto-created from FK, needed for device_groups joins)
CREATE INDEX IF NOT EXISTS idx_sensors_device_id ON v1.sensors (device_id);

-- device_groups junction table (replaces OLD sensor_site_map)
-- Supports many-to-many: devices can belong to multiple groups
CREATE TABLE IF NOT EXISTS v1.device_groups (
    device_id           TEXT NOT NULL,  -- DevEUI reference
    group_id            UUID NOT NULL,
    PRIMARY KEY (device_id, group_id)
);

-- CRITICAL index for "group → devices" reverse lookups
-- Mirrors OLD sensor_site_map pattern: WHERE site_id IN (...)
CREATE INDEX IF NOT EXISTS idx_device_groups_group_id ON v1.device_groups (group_id);

-- =====================================================================
-- PHASE 4: CREATE CONTINUOUS AGGREGATES
-- =====================================================================

-- Temperature continuous aggregate
CREATE MATERIALIZED VIEW v1.hourly_sensor_temperature_stats
WITH (timescaledb.continuous) AS
SELECT
    time_bucket('1 hour', timestamp_utc)                        AS hour,
    sensor_id,
    MIN((payload->>'ValueC')::numeric)                          AS min_temperature_c,
    MAX((payload->>'ValueC')::numeric)                          AS max_temperature_c,
    AVG((payload->>'ValueC')::numeric)                          AS avg_temperature_c,
    stddev((payload->>'ValueC')::numeric)                       AS stddev_temperature_c,
    stats_agg(((payload->>'ValueC')::numeric)::double precision) AS temp_stats_agg,
    percentile_agg(((payload->>'ValueC')::numeric)::double precision) AS temp_percentile_agg,
    count(*)                                                    AS reading_count
FROM v1.sensor_readings
WHERE type = 'Temperature'
  AND payload->>'ValueC' IS NOT NULL
GROUP BY time_bucket('1 hour', timestamp_utc), sensor_id
WITH NO DATA;

SELECT add_continuous_aggregate_policy(
    'v1.hourly_sensor_temperature_stats',
    start_offset      => INTERVAL '1 day',
    end_offset        => INTERVAL '5 minutes',
    schedule_interval => INTERVAL '5 minutes'
);

-- Humidity continuous aggregate
CREATE MATERIALIZED VIEW v1.hourly_sensor_humidity_stats
WITH (timescaledb.continuous) AS
SELECT
    time_bucket('1 hour', timestamp_utc)                          AS hour,
    sensor_id,
    MIN((payload->>'ValueRH')::numeric)                           AS min_humidity_rh,
    MAX((payload->>'ValueRH')::numeric)                           AS max_humidity_rh,
    AVG((payload->>'ValueRH')::numeric)                           AS avg_humidity_rh,
    stddev((payload->>'ValueRH')::numeric)                        AS stddev_humidity_rh,
    stats_agg(((payload->>'ValueRH')::numeric)::double precision) AS humidity_stats_agg,
    percentile_agg(((payload->>'ValueRH')::numeric)::double precision) AS humidity_percentile_agg,
    count(*)                                                      AS reading_count
FROM v1.sensor_readings
WHERE type = 'Humidity'
  AND payload->>'ValueRH' IS NOT NULL
GROUP BY time_bucket('1 hour', timestamp_utc), sensor_id
WITH NO DATA;

SELECT add_continuous_aggregate_policy(
    'v1.hourly_sensor_humidity_stats',
    start_offset      => INTERVAL '1 day',
    end_offset        => INTERVAL '5 minutes',
    schedule_interval => INTERVAL '5 minutes'
);

-- Illumination continuous aggregate
CREATE MATERIALIZED VIEW v1.hourly_sensor_illumination_stats
WITH (timescaledb.continuous) AS
SELECT
    time_bucket('1 hour', timestamp_utc)                           AS hour,
    sensor_id,
    MIN((payload->>'ValueLux')::numeric)                           AS min_lux,
    MAX((payload->>'ValueLux')::numeric)                           AS max_lux,
    AVG((payload->>'ValueLux')::numeric)                           AS avg_lux,
    stddev((payload->>'ValueLux')::numeric)                        AS stddev_lux,
    stats_agg(((payload->>'ValueLux')::numeric)::double precision) AS lux_stats_agg,
    percentile_agg(((payload->>'ValueLux')::numeric)::double precision) AS lux_percentile_agg,
    count(*)                                                       AS reading_count
FROM v1.sensor_readings
WHERE type = 'Illumination'
  AND payload->>'ValueLux' IS NOT NULL
GROUP BY time_bucket('1 hour', timestamp_utc), sensor_id
WITH NO DATA;

SELECT add_continuous_aggregate_policy(
    'v1.hourly_sensor_illumination_stats',
    start_offset      => INTERVAL '1 day',
    end_offset        => INTERVAL '5 minutes',
    schedule_interval => INTERVAL '5 minutes'
);

-- Door continuous aggregate
CREATE MATERIALIZED VIEW v1.hourly_sensor_door_stats
WITH (timescaledb.continuous) AS
SELECT
    time_bucket('1 hour', timestamp_utc)                              AS hour,
    sensor_id,
    SUM((payload->>'Alarm')::integer)                                 AS alarm_count,
    MAX((payload->>'DoorOpen')::integer)                              AS ever_open,
    MAX((payload->>'OpenTimes')::integer)                             AS open_times_max,
    MIN((payload->>'OpenTimes')::integer)                             AS open_times_min,
    SUM((payload->>'OpenDuration')::integer)
        FILTER (WHERE (payload->>'DoorOpen')::integer = 0)            AS open_duration_sum,
    MAX((payload->>'OpenDuration')::integer)
        FILTER (WHERE (payload->>'DoorOpen')::integer = 0)            AS longest_open_duration,
    stats_agg(((payload->>'OpenDuration')::numeric)::double precision)
        FILTER (WHERE (payload->>'DoorOpen')::integer = 0)            AS duration_stats_agg,
    percentile_agg(((payload->>'OpenDuration')::numeric)::double precision)
        FILTER (WHERE (payload->>'DoorOpen')::integer = 0)            AS duration_percentile_agg,
    count(*)                                                           AS reading_count
FROM v1.sensor_readings
WHERE type = 'Door'
  AND payload->>'DoorOpen' IS NOT NULL
GROUP BY time_bucket('1 hour', timestamp_utc), sensor_id
WITH NO DATA;

SELECT add_continuous_aggregate_policy(
    'v1.hourly_sensor_door_stats',
    start_offset      => INTERVAL '1 day',
    end_offset        => INTERVAL '5 minutes',
    schedule_interval => INTERVAL '5 minutes'
);

-- Vibration continuous aggregate
CREATE MATERIALIZED VIEW v1.hourly_sensor_vibration_stats
WITH (timescaledb.continuous) AS
SELECT
    time_bucket('1 hour', timestamp_utc)           AS hour,
    sensor_id,
    (payload->>'VibCount')::integer                AS vib_count,
    MAX((payload->>'WorkMin')::integer)            AS max_work_min,
    SUM((payload->>'Alarm')::integer)              AS alarm_count,
    count(*)                                       AS reading_count
FROM v1.sensor_readings
WHERE type = 'Vibration'
  AND (payload->>'TDC')::integer = 1
GROUP BY time_bucket('1 hour', timestamp_utc), sensor_id, (payload->>'VibCount')::integer
WITH NO DATA;

SELECT add_continuous_aggregate_policy(
    'v1.hourly_sensor_vibration_stats',
    start_offset      => INTERVAL '1 day',
    end_offset        => INTERVAL '5 minutes',
    schedule_interval => INTERVAL '5 minutes'
);

-- =====================================================================
-- PHASE 5: CREATE WINDOW STATS FUNCTIONS
-- =====================================================================

-- Temperature window stats function
CREATE OR REPLACE FUNCTION v1.get_temperature_window_stats(
    p_timezone     TEXT,
    p_window_start timestamp with time zone,
    p_window_end   timestamp with time zone)
RETURNS TABLE(
    group_id            UUID,
    device_id           TEXT,
    sensor_id           UUID,
    actual_window_start text,
    actual_window_end   text,
    min_val             numeric,
    max_val             numeric,
    avg_val             numeric,
    p10                 numeric,
    p25                 numeric,
    median              numeric,
    p75                 numeric,
    p90                 numeric,
    stddev_val          numeric,
    iqr                 numeric,
    range_val           numeric,
    hottest_hour        integer,
    coldest_hour        integer,
    total_readings      bigint)
LANGUAGE sql STABLE AS $function$
    WITH groups_in_tz AS (
        SELECT group_id FROM v1.groups WHERE timezone = p_timezone
    ),
    temp_window AS (
        SELECT
            dg.group_id,
            s.device_id,
            t.sensor_id,
            t.hour,
            t.min_temperature_c,
            t.max_temperature_c,
            t.avg_temperature_c,
            t.temp_stats_agg,
            t.temp_percentile_agg,
            (t.max_temperature_c - t.min_temperature_c) AS temperature_range_c,
            t.reading_count
        FROM v1.hourly_sensor_temperature_stats t
        JOIN v1.sensors s ON s.sensor_id = t.sensor_id
        JOIN v1.device_groups dg ON dg.device_id = s.device_id
        WHERE dg.group_id IN (SELECT group_id FROM groups_in_tz)
          AND t.hour >= p_window_start
          AND t.hour <  p_window_end
    )
    SELECT
        group_id,
        device_id,
        sensor_id,
        to_char(MIN(hour) AT TIME ZONE p_timezone,                   'YYYY-MM-DD"T"HH24:MI:SS') AS actual_window_start,
        to_char((MAX(hour) + INTERVAL '1 hour') AT TIME ZONE p_timezone, 'YYYY-MM-DD"T"HH24:MI:SS') AS actual_window_end,
        ROUND(MIN(min_temperature_c)::numeric, 2)                                        AS min_val,
        ROUND(MAX(max_temperature_c)::numeric, 2)                                        AS max_val,
        ROUND(AVG(avg_temperature_c)::numeric, 2)                                        AS avg_val,
        ROUND(approx_percentile(0.10, rollup(temp_percentile_agg))::numeric, 2)          AS p10,
        ROUND(approx_percentile(0.25, rollup(temp_percentile_agg))::numeric, 2)          AS p25,
        ROUND(approx_percentile(0.50, rollup(temp_percentile_agg))::numeric, 2)          AS median,
        ROUND(approx_percentile(0.75, rollup(temp_percentile_agg))::numeric, 2)          AS p75,
        ROUND(approx_percentile(0.90, rollup(temp_percentile_agg))::numeric, 2)          AS p90,
        ROUND(stddev(rollup(temp_stats_agg))::numeric, 2)                                AS stddev_val,
        ROUND((approx_percentile(0.75, rollup(temp_percentile_agg))
             - approx_percentile(0.25, rollup(temp_percentile_agg)))::numeric, 2)        AS iqr,
        ROUND((MAX(max_temperature_c) - MIN(min_temperature_c))::numeric, 2)            AS range_val,
        EXTRACT(hour FROM (array_agg(hour ORDER BY max_temperature_c DESC))[1] AT TIME ZONE p_timezone)::integer AS hottest_hour,
        EXTRACT(hour FROM (array_agg(hour ORDER BY min_temperature_c ASC))[1]  AT TIME ZONE p_timezone)::integer AS coldest_hour,
        SUM(reading_count)                                                                AS total_readings
    FROM temp_window
    GROUP BY group_id, device_id, sensor_id;
$function$;

-- Humidity window stats function  
CREATE OR REPLACE FUNCTION v1.get_humidity_window_stats(
    p_timezone     TEXT,
    p_window_start timestamp with time zone,
    p_window_end   timestamp with time zone)
RETURNS TABLE(
    group_id            UUID,
    device_id           TEXT,
    sensor_id           UUID,
    actual_window_start text,
    actual_window_end   text,
    min_val             numeric,
    max_val             numeric,
    avg_val             numeric,
    p10                 numeric,
    p25                 numeric,
    median              numeric,
    p75                 numeric,
    p90                 numeric,
    stddev_val          numeric,
    iqr                 numeric,
    range_val           numeric,
    highest_hour        integer,
    lowest_hour         integer,
    total_readings      bigint)
LANGUAGE sql STABLE AS $function$
    WITH groups_in_tz AS (
        SELECT group_id FROM v1.groups WHERE timezone = p_timezone
    ),
    hum_window AS (
        SELECT
            dg.group_id,
            s.device_id,
            h.sensor_id,
            h.hour,
            h.min_humidity_rh,
            h.max_humidity_rh,
            h.avg_humidity_rh,
            h.humidity_stats_agg,
            h.humidity_percentile_agg,
            (h.max_humidity_rh - h.min_humidity_rh) AS humidity_range_rh,
            h.reading_count
        FROM v1.hourly_sensor_humidity_stats h
        JOIN v1.sensors s ON s.sensor_id = h.sensor_id
        JOIN v1.device_groups dg ON dg.device_id = s.device_id
        WHERE dg.group_id IN (SELECT group_id FROM groups_in_tz)
          AND h.hour >= p_window_start
          AND h.hour <  p_window_end
    )
    SELECT
        group_id,
        device_id,
        sensor_id,
        to_char(MIN(hour) AT TIME ZONE p_timezone,                   'YYYY-MM-DD"T"HH24:MI:SS') AS actual_window_start,
        to_char((MAX(hour) + INTERVAL '1 hour') AT TIME ZONE p_timezone, 'YYYY-MM-DD"T"HH24:MI:SS') AS actual_window_end,
        ROUND(MIN(min_humidity_rh)::numeric, 2)                                          AS min_val,
        ROUND(MAX(max_humidity_rh)::numeric, 2)                                          AS max_val,
        ROUND(AVG(avg_humidity_rh)::numeric, 2)                                          AS avg_val,
        ROUND(approx_percentile(0.10, rollup(humidity_percentile_agg))::numeric, 2)      AS p10,
        ROUND(approx_percentile(0.25, rollup(humidity_percentile_agg))::numeric, 2)      AS p25,
        ROUND(approx_percentile(0.50, rollup(humidity_percentile_agg))::numeric, 2)      AS median,
        ROUND(approx_percentile(0.75, rollup(humidity_percentile_agg))::numeric, 2)      AS p75,
        ROUND(approx_percentile(0.90, rollup(humidity_percentile_agg))::numeric, 2)      AS p90,
        ROUND(stddev(rollup(humidity_stats_agg))::numeric, 2)                            AS stddev_val,
        ROUND((approx_percentile(0.75, rollup(humidity_percentile_agg))
             - approx_percentile(0.25, rollup(humidity_percentile_agg)))::numeric, 2)    AS iqr,
        ROUND((MAX(max_humidity_rh) - MIN(min_humidity_rh))::numeric, 2)               AS range_val,
        EXTRACT(hour FROM (array_agg(hour ORDER BY max_humidity_rh DESC))[1] AT TIME ZONE p_timezone)::integer AS highest_hour,
        EXTRACT(hour FROM (array_agg(hour ORDER BY min_humidity_rh ASC))[1]  AT TIME ZONE p_timezone)::integer AS lowest_hour,
        SUM(reading_count)                                                                AS total_readings
    FROM hum_window
    GROUP BY group_id, device_id, sensor_id;
$function$;

-- Illumination window stats function
CREATE OR REPLACE FUNCTION v1.get_illumination_window_stats(
    p_timezone     TEXT,
    p_window_start timestamp with time zone,
    p_window_end   timestamp with time zone)
RETURNS TABLE(
    group_id            UUID,
    device_id           TEXT,
    sensor_id           UUID,
    actual_window_start text,
    actual_window_end   text,
    min_val             numeric,
    max_val             numeric,
    avg_val             numeric,
    p10                 numeric,
    p25                 numeric,
    median              numeric,
    p75                 numeric,
    p90                 numeric,
    stddev_val          numeric,
    iqr                 numeric,
    range_val           numeric,
    brightest_hour      integer,
    darkest_hour        integer,
    total_readings      bigint)
LANGUAGE sql STABLE AS $function$
    WITH groups_in_tz AS (
        SELECT group_id FROM v1.groups WHERE timezone = p_timezone
    ),
    illum_window AS (
        SELECT
            dg.group_id,
            s.device_id,
            t.sensor_id,
            t.hour,
            t.min_lux,
            t.max_lux,
            t.avg_lux,
            t.lux_stats_agg,
            t.lux_percentile_agg,
            (t.max_lux - t.min_lux) AS lux_range,
            t.reading_count
        FROM v1.hourly_sensor_illumination_stats t
        JOIN v1.sensors s ON s.sensor_id = t.sensor_id
        JOIN v1.device_groups dg ON dg.device_id = s.device_id
        WHERE dg.group_id IN (SELECT group_id FROM groups_in_tz)
          AND t.hour >= p_window_start
          AND t.hour <  p_window_end
    )
    SELECT
        group_id,
        device_id,
        sensor_id,
        to_char(MIN(hour) AT TIME ZONE p_timezone,                   'YYYY-MM-DD"T"HH24:MI:SS') AS actual_window_start,
        to_char((MAX(hour) + INTERVAL '1 hour') AT TIME ZONE p_timezone, 'YYYY-MM-DD"T"HH24:MI:SS') AS actual_window_end,
        ROUND(MIN(min_lux)::numeric, 2)                                                  AS min_val,
        ROUND(MAX(max_lux)::numeric, 2)                                                  AS max_val,
        ROUND(AVG(avg_lux)::numeric, 2)                                                  AS avg_val,
        ROUND(approx_percentile(0.10, rollup(lux_percentile_agg))::numeric, 2)           AS p10,
        ROUND(approx_percentile(0.25, rollup(lux_percentile_agg))::numeric, 2)           AS p25,
        ROUND(approx_percentile(0.50, rollup(lux_percentile_agg))::numeric, 2)           AS median,
        ROUND(approx_percentile(0.75, rollup(lux_percentile_agg))::numeric, 2)           AS p75,
        ROUND(approx_percentile(0.90, rollup(lux_percentile_agg))::numeric, 2)           AS p90,
        ROUND(stddev(rollup(lux_stats_agg))::numeric, 2)                                 AS stddev_val,
        ROUND((approx_percentile(0.75, rollup(lux_percentile_agg))
             - approx_percentile(0.25, rollup(lux_percentile_agg)))::numeric, 2)         AS iqr,
        ROUND((MAX(max_lux) - MIN(min_lux))::numeric, 2)                               AS range_val,
        EXTRACT(hour FROM (array_agg(hour ORDER BY max_lux DESC))[1] AT TIME ZONE p_timezone)::integer AS brightest_hour,
        EXTRACT(hour FROM (array_agg(hour ORDER BY min_lux ASC))[1]  AT TIME ZONE p_timezone)::integer AS darkest_hour,
        SUM(reading_count)                                                                AS total_readings
    FROM illum_window
    GROUP BY group_id, device_id, sensor_id;
$function$;

-- Door window stats function
CREATE FUNCTION v1.get_door_window_stats(
    p_timezone     TEXT,
    p_window_start timestamp with time zone,
    p_window_end   timestamp with time zone)
RETURNS TABLE(
    group_id                    UUID,
    device_id                   TEXT,
    sensor_id                   UUID,
    actual_window_start         text,
    actual_window_end           text,
    total_alarms                bigint,
    total_open_times            bigint,
    total_open_duration_minutes bigint,
    longest_open_minutes        integer,
    longest_open_hour           integer,
    avg_open_duration           numeric,
    median_open_duration        numeric,
    stddev_open_duration        numeric,
    p75_open_duration           numeric,
    p90_open_duration           numeric,
    iqr_open_duration           numeric,
    hours_with_open             bigint,
    total_readings              bigint)
LANGUAGE sql STABLE AS $function$
    WITH groups_in_tz AS (
        SELECT group_id FROM v1.groups WHERE timezone = p_timezone
    ),
    door_window AS (
        SELECT
            dg.group_id,
            s.device_id,
            d.sensor_id,
            d.hour,
            d.alarm_count,
            d.ever_open,
            d.open_times_max,
            d.open_times_min,
            d.open_duration_sum,
            d.longest_open_duration,
            d.duration_stats_agg,
            d.duration_percentile_agg,
            d.reading_count
        FROM v1.hourly_sensor_door_stats d
        JOIN v1.sensors s ON s.sensor_id = d.sensor_id
        JOIN v1.device_groups dg ON dg.device_id = s.device_id
        WHERE dg.group_id IN (SELECT group_id FROM groups_in_tz)
          AND d.hour >= p_window_start
          AND d.hour <  p_window_end
    ),
    door_longest AS (
        SELECT DISTINCT ON (group_id, device_id, sensor_id)
            group_id,
            device_id,
            sensor_id,
            longest_open_duration,
            EXTRACT(hour FROM hour AT TIME ZONE p_timezone)::integer AS longest_open_hour
        FROM door_window
        ORDER BY group_id, device_id, sensor_id, longest_open_duration DESC NULLS LAST
    )
    SELECT
        dw.group_id,
        dw.device_id,
        dw.sensor_id,
        to_char(MIN(dw.hour) AT TIME ZONE p_timezone,                       'YYYY-MM-DD"T"HH24:MI:SS') AS actual_window_start,
        to_char((MAX(dw.hour) + INTERVAL '1 hour') AT TIME ZONE p_timezone, 'YYYY-MM-DD"T"HH24:MI:SS') AS actual_window_end,
        SUM(dw.alarm_count)::bigint                                                    AS total_alarms,
        (MAX(dw.open_times_max) - MIN(dw.open_times_min))::bigint                     AS total_open_times,
        SUM(dw.open_duration_sum)::bigint                                              AS total_open_duration_minutes,
        dl.longest_open_duration                                                       AS longest_open_minutes,
        dl.longest_open_hour,
        ROUND(SUM(dw.open_duration_sum)::numeric / NULLIF(MAX(dw.open_times_max) - MIN(dw.open_times_min), 0), 2)   AS avg_open_duration,
        ROUND(approx_percentile(0.50, rollup(dw.duration_percentile_agg))::numeric, 2) AS median_open_duration,
        ROUND(stddev(rollup(dw.duration_stats_agg))::numeric, 2)                       AS stddev_open_duration,
        ROUND(approx_percentile(0.75, rollup(dw.duration_percentile_agg))::numeric, 2) AS p75_open_duration,
        ROUND(approx_percentile(0.90, rollup(dw.duration_percentile_agg))::numeric, 2) AS p90_open_duration,
        ROUND((approx_percentile(0.75, rollup(dw.duration_percentile_agg))
             - approx_percentile(0.25, rollup(dw.duration_percentile_agg)))::numeric, 2) AS iqr_open_duration,
        SUM(dw.ever_open)::bigint                                                      AS hours_with_open,
        SUM(dw.reading_count)::bigint                                                  AS total_readings
    FROM door_window dw
    JOIN door_longest dl USING (group_id, device_id, sensor_id)
    GROUP BY dw.group_id, dw.device_id, dw.sensor_id, dl.longest_open_duration, dl.longest_open_hour;
$function$;

-- Vibration window stats function
CREATE OR REPLACE FUNCTION v1.get_vibration_window_stats(
    p_timezone     TEXT,
    p_window_start timestamp with time zone,
    p_window_end   timestamp with time zone)
RETURNS TABLE(
    group_id            UUID,
    device_id               TEXT,
    sensor_id           UUID,
    actual_window_start text,
    actual_window_end   text,
    total_cycles        bigint,
    total_run_minutes   bigint,
    avg_run_minutes     numeric,
    median_run_minutes  numeric,
    p75_run_minutes     numeric,
    p90_run_minutes     numeric,
    longest_run_minutes integer,
    longest_run_hour    integer,
    avg_off_minutes     numeric,
    peak_cycling_hour   integer,
    peak_cycling_count  integer,
    total_alarms        bigint,
    total_readings      bigint)
LANGUAGE sql STABLE AS $function$
    WITH groups_in_tz AS (
        SELECT group_id FROM v1.groups WHERE timezone = p_timezone
    ),
    vib_window AS (
        SELECT
            dg.group_id,
            s.device_id,
            v.sensor_id,
            v.hour,
            v.vib_count,
            v.max_work_min,
            v.alarm_count,
            v.reading_count
        FROM v1.hourly_sensor_vibration_stats v
        JOIN v1.sensors s ON s.sensor_id = v.sensor_id
        JOIN v1.device_groups dg ON dg.device_id = s.device_id
        WHERE dg.group_id IN (SELECT group_id FROM groups_in_tz)
          AND v.hour >= p_window_start
          AND v.hour <  p_window_end
    ),
    cycle_peaks AS (
        SELECT DISTINCT ON (group_id, device_id, sensor_id, vib_count)
            group_id,
            device_id,
            sensor_id,
            vib_count,
            max_work_min AS peak_work_min,
            hour         AS peak_hour
        FROM vib_window
        ORDER BY group_id, device_id, sensor_id, vib_count, max_work_min DESC NULLS LAST
    ),
    cycle_totals AS (
        SELECT
            group_id,
            device_id,
            sensor_id,
            vib_count,
            SUM(alarm_count)   AS cycle_alarm_count,
            SUM(reading_count) AS cycle_reading_count
        FROM vib_window
        GROUP BY group_id, device_id, sensor_id, vib_count
    ),
    cycles AS (
        SELECT
            cp.group_id,
            cp.device_id,
            cp.sensor_id,
            cp.vib_count,
            cp.peak_work_min,
            cp.peak_hour,
            ct.cycle_alarm_count,
            ct.cycle_reading_count
        FROM cycle_peaks cp
        JOIN cycle_totals ct USING (group_id, device_id, sensor_id, vib_count)
    ),
    window_bounds AS (
        SELECT
            group_id,
            device_id,
            sensor_id,
            MIN(hour) AS first_hour,
            MAX(hour) AS last_hour
        FROM vib_window
        GROUP BY group_id, device_id, sensor_id
    ),
    peak_hour_cycling AS (
        SELECT DISTINCT ON (group_id, device_id, sensor_id)
            group_id,
            device_id,
            sensor_id,
            EXTRACT(hour FROM hour AT TIME ZONE p_timezone)::integer AS peak_cycling_hour,
            cycle_count::integer AS peak_cycling_count
        FROM (
            SELECT
                group_id,
                device_id,
                sensor_id,
                hour,
                COUNT(DISTINCT vib_count) AS cycle_count
            FROM vib_window
            GROUP BY group_id, device_id, sensor_id, hour
        ) h
        ORDER BY group_id, device_id, sensor_id, cycle_count DESC NULLS LAST
    ),
    longest_cycle AS (
        SELECT DISTINCT ON (group_id, device_id, sensor_id)
            group_id,
            device_id,
            sensor_id,
            peak_work_min AS longest_run_minutes,
            EXTRACT(hour FROM peak_hour AT TIME ZONE p_timezone)::integer AS longest_run_hour
        FROM cycles
        ORDER BY group_id, device_id, sensor_id, peak_work_min DESC NULLS LAST
    )
    SELECT
        c.group_id,
        c.device_id,
        c.sensor_id,
        to_char(wb.first_hour AT TIME ZONE p_timezone,                          'YYYY-MM-DD"T"HH24:MI:SS') AS actual_window_start,
        to_char((wb.last_hour + INTERVAL '1 hour') AT TIME ZONE p_timezone,     'YYYY-MM-DD"T"HH24:MI:SS') AS actual_window_end,
        COUNT(*)::bigint                                                         AS total_cycles,
        SUM(c.peak_work_min)::bigint                                             AS total_run_minutes,
        ROUND(AVG(c.peak_work_min)::numeric, 1)                                  AS avg_run_minutes,
        ROUND(approx_percentile(0.50, percentile_agg(c.peak_work_min::double precision))::numeric, 1) AS median_run_minutes,
        ROUND(approx_percentile(0.75, percentile_agg(c.peak_work_min::double precision))::numeric, 1) AS p75_run_minutes,
        ROUND(approx_percentile(0.90, percentile_agg(c.peak_work_min::double precision))::numeric, 1) AS p90_run_minutes,
        lc.longest_run_minutes,
        lc.longest_run_hour,
        ROUND(
            GREATEST(
                (EXTRACT(EPOCH FROM (wb.last_hour + INTERVAL '1 hour' - wb.first_hour)) / 60.0
                    - SUM(c.peak_work_min)::numeric)
                / NULLIF(COUNT(*)::numeric, 0),
                0
            ), 1)                                                                AS avg_off_minutes,
        phc.peak_cycling_hour,
        phc.peak_cycling_count,
        SUM(c.cycle_alarm_count)::bigint                                         AS total_alarms,
        SUM(c.cycle_reading_count)::bigint                                       AS total_readings
    FROM cycles c
    JOIN window_bounds wb      USING (group_id, device_id, sensor_id)
    JOIN longest_cycle lc      USING (group_id, device_id, sensor_id)
    JOIN peak_hour_cycling phc USING (group_id, device_id, sensor_id)
    GROUP BY c.group_id, c.device_id, c.sensor_id,
             wb.first_hour, wb.last_hour,
             lc.longest_run_minutes, lc.longest_run_hour,
             phc.peak_cycling_hour, phc.peak_cycling_count;
$function$;

-- =====================================================================
-- PHASE 6: CREATE TIMEZONE ANALYSIS FUNCTION
-- =====================================================================

-- Timezone analysis stats function
-- Compares current vs previous time windows for all sensor types
CREATE OR REPLACE FUNCTION v1.get_timezone_analysis_stats(
    p_timezone         TEXT,
    p_analysis_datetime timestamp with time zone,
    p_current_window   INTERVAL DEFAULT INTERVAL '24 hours',
    p_previous_window  INTERVAL DEFAULT INTERVAL '24 hours')
RETURNS TABLE(out_group_id UUID, analysis_data jsonb)
LANGUAGE plpgsql STABLE AS $function$
DECLARE
    v_window_start   TIMESTAMPTZ;
    v_window_end     TIMESTAMPTZ;
    v_previous_start TIMESTAMPTZ;
    v_previous_end   TIMESTAMPTZ;
BEGIN
    v_window_end     := date_trunc('hour', p_analysis_datetime);
    v_window_start   := v_window_end   - p_current_window;
    v_previous_end   := v_window_start;
    v_previous_start := v_previous_end - p_previous_window;

    RETURN QUERY
    WITH temp_current AS (
        SELECT * FROM v1.get_temperature_window_stats(p_timezone, v_window_start, v_window_end)
    ),
    temp_previous AS (
        SELECT * FROM v1.get_temperature_window_stats(p_timezone, v_previous_start, v_previous_end)
    ),
    hum_current AS (
        SELECT * FROM v1.get_humidity_window_stats(p_timezone, v_window_start, v_window_end)
    ),
    hum_previous AS (
        SELECT * FROM v1.get_humidity_window_stats(p_timezone, v_previous_start, v_previous_end)
    ),
    illum_current AS (
        SELECT * FROM v1.get_illumination_window_stats(p_timezone, v_window_start, v_window_end)
    ),
    illum_previous AS (
        SELECT * FROM v1.get_illumination_window_stats(p_timezone, v_previous_start, v_previous_end)
    ),
    door_current AS (
        SELECT * FROM v1.get_door_window_stats(p_timezone, v_window_start, v_window_end)
    ),
    door_previous AS (
        SELECT * FROM v1.get_door_window_stats(p_timezone, v_previous_start, v_previous_end)
    ),
    vib_current AS (
        SELECT * FROM v1.get_vibration_window_stats(p_timezone, v_window_start, v_window_end)
    ),
    vib_previous AS (
        SELECT * FROM v1.get_vibration_window_stats(p_timezone, v_previous_start, v_previous_end)
    ),
    all_sensors AS (
        SELECT
            tc.group_id,
            tc.device_id,
            tc.sensor_id,
            'Temperature' AS sensor_type,
            jsonb_build_object(
                'window_start',   tc.actual_window_start,
                'window_end',     tc.actual_window_end,
                'min',            tc.min_val,
                'p10',            tc.p10,
                'p25',            tc.p25,
                'median',         tc.median,
                'avg',            tc.avg_val,
                'p75',            tc.p75,
                'p90',            tc.p90,
                'max',            tc.max_val,
                'stddev',         tc.stddev_val,
                'iqr',            tc.iqr,
                'range',          tc.range_val,
                'hottest_hour',   tc.hottest_hour,
                'coldest_hour',   tc.coldest_hour,
                'total_readings', tc.total_readings
            ) AS current_window,
            jsonb_build_object(
                'window_start',   tp.actual_window_start,
                'window_end',     tp.actual_window_end,
                'min',            tp.min_val,
                'p10',            tp.p10,
                'p25',            tp.p25,
                'median',         tp.median,
                'avg',            tp.avg_val,
                'p75',            tp.p75,
                'p90',            tp.p90,
                'max',            tp.max_val,
                'stddev',         tp.stddev_val,
                'iqr',            tp.iqr,
                'range',          tp.range_val,
                'hottest_hour',   tp.hottest_hour,
                'coldest_hour',   tp.coldest_hour,
                'total_readings', tp.total_readings
            ) AS previous_window,
            jsonb_build_object(
                'min_delta',          tc.min_val      - tp.min_val,
                'p10_delta',          tc.p10          - tp.p10,
                'p25_delta',          tc.p25          - tp.p25,
                'median_delta',       tc.median       - tp.median,
                'avg_delta',          tc.avg_val      - tp.avg_val,
                'p75_delta',          tc.p75          - tp.p75,
                'p90_delta',          tc.p90          - tp.p90,
                'max_delta',          tc.max_val      - tp.max_val,
                'stddev_delta',       tc.stddev_val   - tp.stddev_val,
                'iqr_delta',          tc.iqr          - tp.iqr,
                'range_delta',        tc.range_val    - tp.range_val,
                'hottest_hour_shift', tc.hottest_hour - tp.hottest_hour,
                'coldest_hour_shift', tc.coldest_hour - tp.coldest_hour
            ) AS changes
        FROM temp_current tc
        LEFT JOIN temp_previous tp USING (group_id, device_id, sensor_id)

        UNION ALL

        SELECT
            hc.group_id,
            hc.device_id,
            hc.sensor_id,
            'Humidity' AS sensor_type,
            jsonb_build_object(
                'window_start',   hc.actual_window_start,
                'window_end',     hc.actual_window_end,
                'min',            hc.min_val,
                'p10',            hc.p10,
                'p25',            hc.p25,
                'median',         hc.median,
                'avg',            hc.avg_val,
                'p75',            hc.p75,
                'p90',            hc.p90,
                'max',            hc.max_val,
                'stddev',         hc.stddev_val,
                'iqr',            hc.iqr,
                'range',          hc.range_val,
                'highest_hour',   hc.highest_hour,
                'lowest_hour',    hc.lowest_hour,
                'total_readings', hc.total_readings
            ) AS current_window,
            jsonb_build_object(
                'window_start',   hp.actual_window_start,
                'window_end',     hp.actual_window_end,
                'min',            hp.min_val,
                'p10',            hp.p10,
                'p25',            hp.p25,
                'median',         hp.median,
                'avg',            hp.avg_val,
                'p75',            hp.p75,
                'p90',            hp.p90,
                'max',            hp.max_val,
                'stddev',         hp.stddev_val,
                'iqr',            hp.iqr,
                'range',          hp.range_val,
                'highest_hour',   hp.highest_hour,
                'lowest_hour',    hp.lowest_hour,
                'total_readings', hp.total_readings
            ) AS previous_window,
            jsonb_build_object(
                'min_delta',          hc.min_val      - hp.min_val,
                'p10_delta',          hc.p10          - hp.p10,
                'p25_delta',          hc.p25          - hp.p25,
                'median_delta',       hc.median       - hp.median,
                'avg_delta',          hc.avg_val      - hp.avg_val,
                'p75_delta',          hc.p75          - hp.p75,
                'p90_delta',          hc.p90          - hp.p90,
                'max_delta',          hc.max_val      - hp.max_val,
                'stddev_delta',       hc.stddev_val   - hp.stddev_val,
                'iqr_delta',          hc.iqr          - hp.iqr,
                'range_delta',        hc.range_val    - hp.range_val,
                'highest_hour_shift', hc.highest_hour - hp.highest_hour,
                'lowest_hour_shift',  hc.lowest_hour  - hp.lowest_hour
            ) AS changes
        FROM hum_current hc
        LEFT JOIN hum_previous hp USING (group_id, device_id, sensor_id)

        UNION ALL

        SELECT
            ic.group_id,
            ic.device_id,
            ic.sensor_id,
            'Illumination' AS sensor_type,
            jsonb_build_object(
                'window_start',   ic.actual_window_start,
                'window_end',     ic.actual_window_end,
                'min',            ic.min_val,
                'p10',            ic.p10,
                'p25',            ic.p25,
                'median',         ic.median,
                'avg',            ic.avg_val,
                'p75',            ic.p75,
                'p90',            ic.p90,
                'max',            ic.max_val,
                'stddev',         ic.stddev_val,
                'iqr',            ic.iqr,
                'range',          ic.range_val,
                'brightest_hour', ic.brightest_hour,
                'darkest_hour',   ic.darkest_hour,
                'total_readings', ic.total_readings
            ) AS current_window,
            jsonb_build_object(
                'window_start',   ip.actual_window_start,
                'window_end',     ip.actual_window_end,
                'min',            ip.min_val,
                'p10',            ip.p10,
                'p25',            ip.p25,
                'median',         ip.median,
                'avg',            ip.avg_val,
                'p75',            ip.p75,
                'p90',            ip.p90,
                'max',            ip.max_val,
                'stddev',         ip.stddev_val,
                'iqr',            ip.iqr,
                'range',          ip.range_val,
                'brightest_hour', ip.brightest_hour,
                'darkest_hour',   ip.darkest_hour,
                'total_readings', ip.total_readings
            ) AS previous_window,
            jsonb_build_object(
                'min_delta',            ic.min_val        - ip.min_val,
                'p10_delta',            ic.p10            - ip.p10,
                'p25_delta',            ic.p25            - ip.p25,
                'median_delta',         ic.median         - ip.median,
                'avg_delta',            ic.avg_val        - ip.avg_val,
                'p75_delta',            ic.p75            - ip.p75,
                'p90_delta',            ic.p90            - ip.p90,
                'max_delta',            ic.max_val        - ip.max_val,
                'stddev_delta',         ic.stddev_val     - ip.stddev_val,
                'iqr_delta',            ic.iqr            - ip.iqr,
                'range_delta',          ic.range_val      - ip.range_val,
                'brightest_hour_shift', ic.brightest_hour - ip.brightest_hour,
                'darkest_hour_shift',   ic.darkest_hour   - ip.darkest_hour
            ) AS changes
        FROM illum_current ic
        LEFT JOIN illum_previous ip USING (group_id, device_id, sensor_id)

        UNION ALL

        SELECT
            dc.group_id,
            dc.device_id,
            dc.sensor_id,
            'Door' AS sensor_type,
            jsonb_build_object(
                'window_start',                dc.actual_window_start,
                'window_end',                  dc.actual_window_end,
                'total_alarms',                dc.total_alarms,
                'total_open_times',            dc.total_open_times,
                'total_open_duration_minutes', dc.total_open_duration_minutes,
                'longest_open_minutes',        dc.longest_open_minutes,
                'longest_open_hour',           dc.longest_open_hour,
                'avg',                         dc.avg_open_duration,
                'median',                      dc.median_open_duration,
                'stddev',                      dc.stddev_open_duration,
                'p75',                         dc.p75_open_duration,
                'p90',                         dc.p90_open_duration,
                'iqr',                         dc.iqr_open_duration,
                'hours_with_open',             dc.hours_with_open,
                'total_readings',              dc.total_readings
            ) AS current_window,
            jsonb_build_object(
                'window_start',                dp.actual_window_start,
                'window_end',                  dp.actual_window_end,
                'total_alarms',                dp.total_alarms,
                'total_open_times',            dp.total_open_times,
                'total_open_duration_minutes', dp.total_open_duration_minutes,
                'longest_open_minutes',        dp.longest_open_minutes,
                'longest_open_hour',           dp.longest_open_hour,
                'avg',                         dp.avg_open_duration,
                'median',                      dp.median_open_duration,
                'stddev',                      dp.stddev_open_duration,
                'p75',                         dp.p75_open_duration,
                'p90',                         dp.p90_open_duration,
                'iqr',                         dp.iqr_open_duration,
                'hours_with_open',             dp.hours_with_open,
                'total_readings',              dp.total_readings
            ) AS previous_window,
            jsonb_build_object(
                'alarm_delta',           dc.total_alarms                - dp.total_alarms,
                'open_times_delta',      dc.total_open_times            - dp.total_open_times,
                'open_duration_delta',   dc.total_open_duration_minutes - dp.total_open_duration_minutes,
                'longest_open_delta',    dc.longest_open_minutes        - dp.longest_open_minutes,
                'avg_delta',             dc.avg_open_duration           - dp.avg_open_duration,
                'median_delta',          dc.median_open_duration        - dp.median_open_duration,
                'stddev_delta',          dc.stddev_open_duration        - dp.stddev_open_duration,
                'p75_delta',             dc.p75_open_duration           - dp.p75_open_duration,
                'p90_delta',             dc.p90_open_duration           - dp.p90_open_duration,
                'iqr_delta',             dc.iqr_open_duration           - dp.iqr_open_duration,
                'hours_with_open_delta', dc.hours_with_open             - dp.hours_with_open
            ) AS changes
        FROM door_current dc
        LEFT JOIN door_previous dp USING (group_id, device_id, sensor_id)

        UNION ALL

        SELECT
            vc.group_id,
            vc.device_id,
            vc.sensor_id,
            'Vibration' AS sensor_type,
            jsonb_build_object(
                'window_start',        vc.actual_window_start,
                'window_end',          vc.actual_window_end,
                'total_cycles',        vc.total_cycles,
                'total_run_minutes',   vc.total_run_minutes,
                'avg_run_minutes',     vc.avg_run_minutes,
                'median_run_minutes',  vc.median_run_minutes,
                'p75_run_minutes',     vc.p75_run_minutes,
                'p90_run_minutes',     vc.p90_run_minutes,
                'longest_run_minutes', vc.longest_run_minutes,
                'longest_run_hour',    vc.longest_run_hour,
                'avg_off_minutes',     vc.avg_off_minutes,
                'peak_cycling_hour',   vc.peak_cycling_hour,
                'peak_cycling_count',  vc.peak_cycling_count,
                'total_alarms',        vc.total_alarms,
                'total_readings',      vc.total_readings
            ) AS current_window,
            jsonb_build_object(
                'window_start',        vp.actual_window_start,
                'window_end',          vp.actual_window_end,
                'total_cycles',        vp.total_cycles,
                'total_run_minutes',   vp.total_run_minutes,
                'avg_run_minutes',     vp.avg_run_minutes,
                'median_run_minutes',  vp.median_run_minutes,
                'p75_run_minutes',     vp.p75_run_minutes,
                'p90_run_minutes',     vp.p90_run_minutes,
                'longest_run_minutes', vp.longest_run_minutes,
                'longest_run_hour',    vp.longest_run_hour,
                'avg_off_minutes',     vp.avg_off_minutes,
                'peak_cycling_hour',   vp.peak_cycling_hour,
                'peak_cycling_count',  vp.peak_cycling_count,
                'total_alarms',        vp.total_alarms,
                'total_readings',      vp.total_readings
            ) AS previous_window,
            jsonb_build_object(
                'total_cycles_delta',         vc.total_cycles        - vp.total_cycles,
                'total_run_minutes_delta',    vc.total_run_minutes   - vp.total_run_minutes,
                'avg_run_minutes_delta',      vc.avg_run_minutes     - vp.avg_run_minutes,
                'median_run_minutes_delta',   vc.median_run_minutes  - vp.median_run_minutes,
                'p75_run_minutes_delta',      vc.p75_run_minutes     - vp.p75_run_minutes,
                'p90_run_minutes_delta',      vc.p90_run_minutes     - vp.p90_run_minutes,
                'longest_run_minutes_delta',  vc.longest_run_minutes - vp.longest_run_minutes,
                'longest_run_hour_shift',     vc.longest_run_hour    - vp.longest_run_hour,
                'avg_off_minutes_delta',      vc.avg_off_minutes     - vp.avg_off_minutes,
                'peak_cycling_count_delta',   vc.peak_cycling_count  - vp.peak_cycling_count,
                'peak_cycling_hour_shift',    vc.peak_cycling_hour   - vp.peak_cycling_hour,
                'alarm_delta',                vc.total_alarms        - vp.total_alarms
            ) AS changes
        FROM vib_current vc
        LEFT JOIN vib_previous vp USING (group_id, device_id, sensor_id)
    ),
    sensors_json AS (
        SELECT
            group_id,
            jsonb_agg(
                jsonb_build_object(
                    'sensor_id',       sensor_id,
                    'device_id',       device_id,
                    'sensor_type',     sensor_type,
                    'current_window',  current_window,
                    'previous_window', previous_window,
                    'changes',         changes
                ) ORDER BY device_id, sensor_type
            ) AS sensors
        FROM all_sensors
        GROUP BY group_id
    )
    SELECT
        sj.group_id,
        jsonb_build_object(
            'group_id',              sj.group_id,
            'timezone',              p_timezone,
            'analysis_requested_at', p_analysis_datetime AT TIME ZONE p_timezone,
            'requested_period',      jsonb_build_object(
                'current_window',  p_current_window::TEXT,
                'current_start',   v_window_start   AT TIME ZONE p_timezone,
                'current_end',     v_window_end     AT TIME ZONE p_timezone,
                'previous_window', p_previous_window::TEXT,
                'previous_start',  v_previous_start AT TIME ZONE p_timezone,
                'previous_end',    v_previous_end   AT TIME ZONE p_timezone
            ),
            'sensors', sj.sensors
        ) AS analysis_data
    FROM sensors_json sj;
END;
$function$;

-- =====================================================================
-- DEPLOYMENT COMPLETE
-- =====================================================================
-- Next steps:
-- 1. Populate configuration tables (accounts, groups, devices, sensors, device_groups)
-- 2. Start ingesting data to v1.sensor_readings and v1.gateway_readings
-- 3. Continuous aggregates will refresh automatically every 5 minutes
-- 4. Query window functions and timezone analysis function
-- =====================================================================
