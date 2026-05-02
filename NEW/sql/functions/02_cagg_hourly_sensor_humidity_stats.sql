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
