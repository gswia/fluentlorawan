-- Graph RAG version: Direct sensor ID filtering (no group/timezone filtering)
CREATE OR REPLACE FUNCTION v1.get_temperature_window_stats2(
    p_sensor_ids   UUID[],
    p_window_start TIMESTAMPTZ,
    p_window_end   TIMESTAMPTZ)
RETURNS TABLE(
    sensor_id           UUID,
    device_id           TEXT,
    actual_window_start TIMESTAMPTZ,
    actual_window_end   TIMESTAMPTZ,
    min_val             NUMERIC,
    max_val             NUMERIC,
    avg_val             NUMERIC,
    p10                 NUMERIC,
    p25                 NUMERIC,
    median              NUMERIC,
    p75                 NUMERIC,
    p90                 NUMERIC,
    stddev_val          NUMERIC,
    iqr                 NUMERIC,
    range_val           NUMERIC,
    hottest_hour        INTEGER,
    coldest_hour        INTEGER,
    total_readings      BIGINT)
LANGUAGE sql STABLE AS $function$
    WITH temp_window AS (
        SELECT
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
        WHERE t.sensor_id = ANY(p_sensor_ids)
          AND t.hour >= p_window_start
          AND t.hour <  p_window_end
    )
    SELECT
        sensor_id,
        device_id,
        MIN(hour) AS actual_window_start,
        MAX(hour) + INTERVAL '1 hour' AS actual_window_end,
        ROUND(MIN(min_temperature_c)::NUMERIC, 2) AS min_val,
        ROUND(MAX(max_temperature_c)::NUMERIC, 2) AS max_val,
        ROUND(AVG(avg_temperature_c)::NUMERIC, 2) AS avg_val,
        ROUND(approx_percentile(0.10, rollup(temp_percentile_agg))::NUMERIC, 2) AS p10,
        ROUND(approx_percentile(0.25, rollup(temp_percentile_agg))::NUMERIC, 2) AS p25,
        ROUND(approx_percentile(0.50, rollup(temp_percentile_agg))::NUMERIC, 2) AS median,
        ROUND(approx_percentile(0.75, rollup(temp_percentile_agg))::NUMERIC, 2) AS p75,
        ROUND(approx_percentile(0.90, rollup(temp_percentile_agg))::NUMERIC, 2) AS p90,
        ROUND(stddev(rollup(temp_stats_agg))::NUMERIC, 2) AS stddev_val,
        ROUND((approx_percentile(0.75, rollup(temp_percentile_agg))
             - approx_percentile(0.25, rollup(temp_percentile_agg)))::NUMERIC, 2) AS iqr,
        ROUND((MAX(max_temperature_c) - MIN(min_temperature_c))::NUMERIC, 2) AS range_val,
        EXTRACT(hour FROM (array_agg(hour ORDER BY max_temperature_c DESC))[1])::INTEGER AS hottest_hour,
        EXTRACT(hour FROM (array_agg(hour ORDER BY min_temperature_c ASC))[1])::INTEGER AS coldest_hour,
        SUM(reading_count) AS total_readings
    FROM temp_window
    GROUP BY sensor_id, device_id;
$function$;
