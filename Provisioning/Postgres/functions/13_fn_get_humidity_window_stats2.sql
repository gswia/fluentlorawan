-- Graph RAG version: Direct sensor ID filtering (no group/timezone filtering)
CREATE OR REPLACE FUNCTION v1.get_humidity_window_stats2(
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
    highest_hour        INTEGER,
    lowest_hour         INTEGER,
    total_readings      BIGINT)
LANGUAGE sql STABLE AS $function$
    WITH hum_window AS (
        SELECT
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
        WHERE h.sensor_id = ANY(p_sensor_ids)
          AND h.hour >= p_window_start
          AND h.hour <  p_window_end
    )
    SELECT
        sensor_id,
        device_id,
        MIN(hour) AS actual_window_start,
        MAX(hour) + INTERVAL '1 hour' AS actual_window_end,
        ROUND(MIN(min_humidity_rh)::NUMERIC, 2) AS min_val,
        ROUND(MAX(max_humidity_rh)::NUMERIC, 2) AS max_val,
        ROUND(AVG(avg_humidity_rh)::NUMERIC, 2) AS avg_val,
        ROUND(approx_percentile(0.10, rollup(humidity_percentile_agg))::NUMERIC, 2) AS p10,
        ROUND(approx_percentile(0.25, rollup(humidity_percentile_agg))::NUMERIC, 2) AS p25,
        ROUND(approx_percentile(0.50, rollup(humidity_percentile_agg))::NUMERIC, 2) AS median,
        ROUND(approx_percentile(0.75, rollup(humidity_percentile_agg))::NUMERIC, 2) AS p75,
        ROUND(approx_percentile(0.90, rollup(humidity_percentile_agg))::NUMERIC, 2) AS p90,
        ROUND(stddev(rollup(humidity_stats_agg))::NUMERIC, 2) AS stddev_val,
        ROUND((approx_percentile(0.75, rollup(humidity_percentile_agg))
             - approx_percentile(0.25, rollup(humidity_percentile_agg)))::NUMERIC, 2) AS iqr,
        ROUND((MAX(max_humidity_rh) - MIN(min_humidity_rh))::NUMERIC, 2) AS range_val,
        EXTRACT(hour FROM (array_agg(hour ORDER BY max_humidity_rh DESC))[1])::INTEGER AS highest_hour,
        EXTRACT(hour FROM (array_agg(hour ORDER BY min_humidity_rh ASC))[1])::INTEGER AS lowest_hour,
        SUM(reading_count) AS total_readings
    FROM hum_window
    GROUP BY sensor_id, device_id;
$function$;
