-- Graph RAG version: Direct sensor ID filtering (no group/timezone filtering)
CREATE OR REPLACE FUNCTION v1.get_door_window_stats2(
    p_sensor_ids   UUID[],
    p_window_start TIMESTAMPTZ,
    p_window_end   TIMESTAMPTZ)
RETURNS TABLE(
    sensor_id                   UUID,
    device_id                   TEXT,
    actual_window_start         TIMESTAMPTZ,
    actual_window_end           TIMESTAMPTZ,
    total_alarms                BIGINT,
    total_open_times            BIGINT,
    total_open_duration_minutes BIGINT,
    longest_open_minutes        INTEGER,
    longest_open_hour           INTEGER,
    avg_open_duration           NUMERIC,
    median_open_duration        NUMERIC,
    stddev_open_duration        NUMERIC,
    p75_open_duration           NUMERIC,
    p90_open_duration           NUMERIC,
    iqr_open_duration           NUMERIC,
    hours_with_open             BIGINT,
    total_readings              BIGINT)
LANGUAGE sql STABLE AS $function$
    WITH door_window AS (
        SELECT
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
        WHERE d.sensor_id = ANY(p_sensor_ids)
          AND d.hour >= p_window_start
          AND d.hour <  p_window_end
    ),
    door_longest AS (
        SELECT DISTINCT ON (device_id, sensor_id)
            device_id,
            sensor_id,
            longest_open_duration,
            EXTRACT(hour FROM hour)::INTEGER AS longest_open_hour
        FROM door_window
        ORDER BY device_id, sensor_id, longest_open_duration DESC NULLS LAST
    )
    SELECT
        dw.sensor_id,
        dw.device_id,
        MIN(dw.hour) AS actual_window_start,
        MAX(dw.hour) + INTERVAL '1 hour' AS actual_window_end,
        SUM(dw.alarm_count)::BIGINT AS total_alarms,
        (MAX(dw.open_times_max) - MIN(dw.open_times_min))::BIGINT AS total_open_times,
        SUM(dw.open_duration_sum)::BIGINT AS total_open_duration_minutes,
        dl.longest_open_duration AS longest_open_minutes,
        dl.longest_open_hour,
        ROUND(SUM(dw.open_duration_sum)::NUMERIC / NULLIF(MAX(dw.open_times_max) - MIN(dw.open_times_min), 0), 2) AS avg_open_duration,
        ROUND(approx_percentile(0.50, rollup(dw.duration_percentile_agg))::NUMERIC, 2) AS median_open_duration,
        ROUND(stddev(rollup(dw.duration_stats_agg))::NUMERIC, 2) AS stddev_open_duration,
        ROUND(approx_percentile(0.75, rollup(dw.duration_percentile_agg))::NUMERIC, 2) AS p75_open_duration,
        ROUND(approx_percentile(0.90, rollup(dw.duration_percentile_agg))::NUMERIC, 2) AS p90_open_duration,
        ROUND((approx_percentile(0.75, rollup(dw.duration_percentile_agg))
             - approx_percentile(0.25, rollup(dw.duration_percentile_agg)))::NUMERIC, 2) AS iqr_open_duration,
        SUM(dw.ever_open)::BIGINT AS hours_with_open,
        SUM(dw.reading_count) AS total_readings
    FROM door_window dw
    LEFT JOIN door_longest dl ON dw.sensor_id = dl.sensor_id AND dw.device_id = dl.device_id
    GROUP BY dw.sensor_id, dw.device_id, dl.longest_open_duration, dl.longest_open_hour;
$function$;
