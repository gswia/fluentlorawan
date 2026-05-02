-- Groups by (hour, sensor_id, vib_count) so each row represents one HVAC run cycle
-- within a given hour bucket. A cycle spanning an hour boundary will appear in
-- multiple hour buckets; the window stats function resolves the true peak by taking
-- MAX(max_work_min) across all buckets for the same vib_count.
--
-- TDC=1 filter: only scheduled uplinks are included. TDC=0 interrupt packets fire
-- mid-cycle with a partial WorkMin value and would corrupt cycle duration tracking.
--
-- Requires TimescaleDB >= 2.7 for JSONB expression support in GROUP BY.

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
