-- median, stddev, p75, p90, iqr: computed via rollup() over duration_percentile_agg
-- and duration_stats_agg tdigest sketches stored in hourly_sensor_door_stats.
-- This matches the Temperature/Humidity/Illumination pattern exactly.
-- avg: SUM(open_duration_sum) / total_open_times — true mean event duration
-- (no mean() accessor exists for statssummary1d, so avg is derived from raw counters).

CREATE FUNCTION v1.get_door_window_stats(
    p_timezone     TEXT,
    p_window_start timestamp with time zone,
    p_window_end   timestamp with time zone)
RETURNS TABLE(
    group_id                    UUID,
    device_id                   UUID,
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
    -- door_longest selects the single hour with the longest open event per sensor
    -- (used to report longest_open_minutes + longest_open_hour)
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
