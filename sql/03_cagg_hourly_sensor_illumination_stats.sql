CREATE MATERIALIZED VIEW hourly_sensor_illumination_stats
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
FROM sensor_readings
WHERE type = 'Illumination'
  AND payload->>'ValueLux' IS NOT NULL
GROUP BY time_bucket('1 hour', timestamp_utc), sensor_id
WITH NO DATA;

SELECT add_continuous_aggregate_policy(
    'hourly_sensor_illumination_stats',
    start_offset      => INTERVAL '1 day',
    end_offset        => INTERVAL '5 minutes',
    schedule_interval => INTERVAL '5 minutes'
);
