CREATE OR REPLACE FUNCTION v1.get_humidity_window_stats(
    p_timezone     TEXT,
    p_window_start timestamp with time zone,
    p_window_end   timestamp with time zone)
RETURNS TABLE(
    group_id            UUID,
    device_id           UUID,
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
