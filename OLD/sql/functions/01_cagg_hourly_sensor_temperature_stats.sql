CREATE MATERIALIZED VIEW hourly_sensor_temperature_stats
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
FROM sensor_readings
WHERE type = 'Temperature'
  AND payload->>'ValueC' IS NOT NULL
GROUP BY time_bucket('1 hour', timestamp_utc), sensor_id
WITH NO DATA;

SELECT add_continuous_aggregate_policy(
    'hourly_sensor_temperature_stats',
    start_offset      => INTERVAL '1 day',
    end_offset        => INTERVAL '5 minutes',
    schedule_interval => INTERVAL '5 minutes'
);
