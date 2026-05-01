-- Aggregates hourly_sensor_vibration_stats into per-sensor 24-hour window stats.
--
-- Two-level aggregation:
--   Level 1 (cycle_peaks): for each VibCount, find the cagg row with the highest
--     max_work_min — that is the true cycle peak duration (cycles spanning hour
--     boundaries have their peak in their final hour bucket).
--   Level 2 (final SELECT): aggregate per-cycle peaks into window-level stats.
--     percentile_agg(peak_work_min) computes a true cycle-duration distribution,
--     not a distribution over raw packet WorkMin values.
--
-- longest_run_hour: local hour of the cagg bucket that contains the cycle's peak
--   max_work_min — i.e. the hour the cycle ended / HVAC stopped.

CREATE OR REPLACE FUNCTION get_vibration_window_stats(
    p_timezone     character varying,
    p_window_start timestamp with time zone,
    p_window_end   timestamp with time zone)
RETURNS TABLE(
    site_id             character varying,
    device_id           character varying,
    sensor_id           character varying,
    actual_window_start text,
    actual_window_end   text,
    total_cycles        bigint,
    total_run_minutes   bigint,
    avg_run_minutes     numeric,
    median_run_minutes  numeric,
    p75_run_minutes     numeric,
    p90_run_minutes     numeric,
    longest_run_minutes integer,
    longest_run_hour    integer,
    avg_off_minutes     numeric,
    peak_cycling_hour   integer,
    peak_cycling_count  integer,
    total_alarms        bigint,
    total_readings      bigint)
LANGUAGE sql STABLE AS $function$
    WITH sites_in_tz AS (
        SELECT s.site_id AS tz_site_id
        FROM sites s
        WHERE s.timezone = p_timezone
    ),
    vib_window AS (
        SELECT
            sm.site_id,
            sm.device_id,
            v.sensor_id,
            v.hour,
            v.vib_count,
            v.max_work_min,
            v.alarm_count,
            v.reading_count
        FROM hourly_sensor_vibration_stats v
        JOIN sensor_site_map sm ON sm.sensor_id = v.sensor_id
        WHERE sm.site_id IN (SELECT tz_site_id FROM sites_in_tz)
          AND v.hour >= p_window_start
          AND v.hour <  p_window_end
    ),
    -- For each cycle (vib_count), pick the cagg row with the highest max_work_min.
    -- The hour of that row is when the cycle peaked / ended.
    cycle_peaks AS (
        SELECT DISTINCT ON (site_id, device_id, sensor_id, vib_count)
            site_id,
            device_id,
            sensor_id,
            vib_count,
            max_work_min AS peak_work_min,
            hour         AS peak_hour
        FROM vib_window
        ORDER BY site_id, device_id, sensor_id, vib_count, max_work_min DESC NULLS LAST
    ),
    -- Sum alarms and readings across all hour buckets belonging to each cycle.
    cycle_totals AS (
        SELECT
            site_id,
            device_id,
            sensor_id,
            vib_count,
            SUM(alarm_count)   AS cycle_alarm_count,
            SUM(reading_count) AS cycle_reading_count
        FROM vib_window
        GROUP BY site_id, device_id, sensor_id, vib_count
    ),
    cycles AS (
        SELECT
            cp.site_id,
            cp.device_id,
            cp.sensor_id,
            cp.vib_count,
            cp.peak_work_min,
            cp.peak_hour,
            ct.cycle_alarm_count,
            ct.cycle_reading_count
        FROM cycle_peaks cp
        JOIN cycle_totals ct USING (site_id, device_id, sensor_id, vib_count)
    ),
    window_bounds AS (
        SELECT
            site_id,
            device_id,
            sensor_id,
            MIN(hour) AS first_hour,
            MAX(hour) AS last_hour
        FROM vib_window
        GROUP BY site_id, device_id, sensor_id
    ),
    -- Hour with the most distinct cycles — primary short-cycling indicator.
    peak_hour_cycling AS (
        SELECT DISTINCT ON (site_id, device_id, sensor_id)
            site_id,
            device_id,
            sensor_id,
            EXTRACT(hour FROM hour AT TIME ZONE p_timezone)::integer AS peak_cycling_hour,
            cycle_count::integer AS peak_cycling_count
        FROM (
            SELECT
                site_id,
                device_id,
                sensor_id,
                hour,
                COUNT(DISTINCT vib_count) AS cycle_count
            FROM vib_window
            GROUP BY site_id, device_id, sensor_id, hour
        ) h
        ORDER BY site_id, device_id, sensor_id, cycle_count DESC NULLS LAST
    ),
    -- The single cycle with the longest run; its peak_hour gives longest_run_hour.
    longest_cycle AS (
        SELECT DISTINCT ON (site_id, device_id, sensor_id)
            site_id,
            device_id,
            sensor_id,
            peak_work_min AS longest_run_minutes,
            EXTRACT(hour FROM peak_hour AT TIME ZONE p_timezone)::integer AS longest_run_hour
        FROM cycles
        ORDER BY site_id, device_id, sensor_id, peak_work_min DESC NULLS LAST
    )
    SELECT
        c.site_id,
        c.device_id,
        c.sensor_id,
        to_char(wb.first_hour AT TIME ZONE p_timezone,                          'YYYY-MM-DD"T"HH24:MI:SS') AS actual_window_start,
        to_char((wb.last_hour + INTERVAL '1 hour') AT TIME ZONE p_timezone,     'YYYY-MM-DD"T"HH24:MI:SS') AS actual_window_end,
        COUNT(*)::bigint                                                         AS total_cycles,
        SUM(c.peak_work_min)::bigint                                             AS total_run_minutes,
        ROUND(AVG(c.peak_work_min)::numeric, 1)                                  AS avg_run_minutes,
        ROUND(approx_percentile(0.50, percentile_agg(c.peak_work_min::double precision))::numeric, 1) AS median_run_minutes,
        ROUND(approx_percentile(0.75, percentile_agg(c.peak_work_min::double precision))::numeric, 1) AS p75_run_minutes,
        ROUND(approx_percentile(0.90, percentile_agg(c.peak_work_min::double precision))::numeric, 1) AS p90_run_minutes,
        lc.longest_run_minutes,
        lc.longest_run_hour,
        -- avg_off_minutes: mean compressor rest time between cycles.
        -- Formula: (observed_window_minutes - total_run_minutes) / total_cycles.
        -- A value below ~10 minutes indicates the compressor is not getting adequate rest.
        ROUND(
            GREATEST(
                (EXTRACT(EPOCH FROM (wb.last_hour + INTERVAL '1 hour' - wb.first_hour)) / 60.0
                    - SUM(c.peak_work_min)::numeric)
                / NULLIF(COUNT(*)::numeric, 0),
                0
            ), 1)                                                                AS avg_off_minutes,
        phc.peak_cycling_hour,
        phc.peak_cycling_count,
        SUM(c.cycle_alarm_count)::bigint                                         AS total_alarms,
        SUM(c.cycle_reading_count)::bigint                                       AS total_readings
    FROM cycles c
    JOIN window_bounds wb      USING (site_id, device_id, sensor_id)
    JOIN longest_cycle lc      USING (site_id, device_id, sensor_id)
    JOIN peak_hour_cycling phc USING (site_id, device_id, sensor_id)
    GROUP BY c.site_id, c.device_id, c.sensor_id,
             wb.first_hour, wb.last_hour,
             lc.longest_run_minutes, lc.longest_run_hour,
             phc.peak_cycling_hour, phc.peak_cycling_count;
$function$;
