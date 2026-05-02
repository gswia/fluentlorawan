-- open_duration_sum / longest_open_duration: FILTER (WHERE DoorOpen=0)
-- The firmware sends two packets per open event; the open packet carries the
-- *previous* event's stale duration, so we exclude open packets to avoid
-- double-counting.  All percentile/distribution metrics are computed over
-- individual close-packet OpenDuration values via tdigest sketches
-- (duration_percentile_agg, duration_stats_agg) — matching the same pattern
-- used by Temperature, Humidity, and Illumination.

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
