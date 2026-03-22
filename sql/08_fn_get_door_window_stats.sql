-- median, stddev, p75, p90, iqr: computed via rollup() over duration_percentile_agg
-- and duration_stats_agg tdigest sketches stored in hourly_sensor_door_stats.
-- This matches the Temperature/Humidity/Illumination pattern exactly.
-- avg: SUM(open_duration_sum) / total_open_times — true mean event duration
-- (no mean() accessor exists for statssummary1d, so avg is derived from raw counters).

CREATE FUNCTION get_door_window_stats(
    p_timezone     character varying,
    p_window_start timestamp with time zone,
    p_window_end   timestamp with time zone)
RETURNS TABLE(
    site_id                     character varying,
    device_id                   character varying,
    sensor_id                   character varying,
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
    WITH sites_in_tz AS (
        SELECT s.site_id AS tz_site_id
        FROM sites s
        WHERE s.timezone = p_timezone
    ),
    door_window AS (
        SELECT
            sm.site_id,
            sm.device_id,
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
        FROM hourly_sensor_door_stats d
        JOIN sensor_site_map sm ON sm.sensor_id = d.sensor_id
        WHERE sm.site_id IN (SELECT tz_site_id FROM sites_in_tz)
          AND d.hour >= p_window_start
          AND d.hour <  p_window_end
    ),
    -- door_longest selects the single hour with the longest open event per sensor
    -- (used to report longest_open_minutes + longest_open_hour)
    door_longest AS (
        SELECT DISTINCT ON (site_id, device_id, sensor_id)
            site_id,
            device_id,
            sensor_id,
            longest_open_duration,
            EXTRACT(hour FROM hour AT TIME ZONE p_timezone)::integer AS longest_open_hour
        FROM door_window
        ORDER BY site_id, device_id, sensor_id, longest_open_duration DESC NULLS LAST
    )
    SELECT
        dw.site_id,
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
    JOIN door_longest dl USING (site_id, device_id, sensor_id)
    GROUP BY dw.site_id, dw.device_id, dw.sensor_id, dl.longest_open_duration, dl.longest_open_hour;
$function$;
