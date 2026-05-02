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

CREATE OR REPLACE FUNCTION v1.get_vibration_window_stats(
    p_timezone     TEXT,
    p_window_start timestamp with time zone,
    p_window_end   timestamp with time zone)
RETURNS TABLE(
    group_id            UUID,
    device_id           UUID,
    sensor_id           UUID,
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
    WITH groups_in_tz AS (
        SELECT group_id FROM v1.groups WHERE timezone = p_timezone
    ),
    vib_window AS (
        SELECT
            dg.group_id,
            s.device_id,
            v.sensor_id,
            v.hour,
            v.vib_count,
            v.max_work_min,
            v.alarm_count,
            v.reading_count
        FROM v1.hourly_sensor_vibration_stats v
        JOIN v1.sensors s ON s.sensor_id = v.sensor_id
        JOIN v1.device_groups dg ON dg.device_id = s.device_id
        WHERE dg.group_id IN (SELECT group_id FROM groups_in_tz)
          AND v.hour >= p_window_start
          AND v.hour <  p_window_end
    ),
    -- For each cycle (vib_count), pick the cagg row with the highest max_work_min.
    -- The hour of that row is when the cycle peaked / ended.
    cycle_peaks AS (
        SELECT DISTINCT ON (group_id, device_id, sensor_id, vib_count)
            group_id,
            device_id,
            sensor_id,
            vib_count,
            max_work_min AS peak_work_min,
            hour         AS peak_hour
        FROM vib_window
        ORDER BY group_id, device_id, sensor_id, vib_count, max_work_min DESC NULLS LAST
    ),
    -- Sum alarms and readings across all hour buckets belonging to each cycle.
    cycle_totals AS (
        SELECT
            group_id,
            device_id,
            sensor_id,
            vib_count,
            SUM(alarm_count)   AS cycle_alarm_count,
            SUM(reading_count) AS cycle_reading_count
        FROM vib_window
        GROUP BY group_id, device_id, sensor_id, vib_count
    ),
    cycles AS (
        SELECT
            cp.group_id,
            cp.device_id,
            cp.sensor_id,
            cp.vib_count,
            cp.peak_work_min,
            cp.peak_hour,
            ct.cycle_alarm_count,
            ct.cycle_reading_count
        FROM cycle_peaks cp
        JOIN cycle_totals ct USING (group_id, device_id, sensor_id, vib_count)
    ),
    window_bounds AS (
        SELECT
            group_id,
            device_id,
            sensor_id,
            MIN(hour) AS first_hour,
            MAX(hour) AS last_hour
        FROM vib_window
        GROUP BY group_id, device_id, sensor_id
    ),
    -- Hour with the most distinct cycles — primary short-cycling indicator.
    peak_hour_cycling AS (
        SELECT DISTINCT ON (group_id, device_id, sensor_id)
            group_id,
            device_id,
            sensor_id,
            EXTRACT(hour FROM hour AT TIME ZONE p_timezone)::integer AS peak_cycling_hour,
            cycle_count::integer AS peak_cycling_count
        FROM (
            SELECT
                group_id,
                device_id,
                sensor_id,
                hour,
                COUNT(DISTINCT vib_count) AS cycle_count
            FROM vib_window
            GROUP BY group_id, device_id, sensor_id, hour
        ) h
        ORDER BY group_id, device_id, sensor_id, cycle_count DESC NULLS LAST
    ),
    -- The single cycle with the longest run; its peak_hour gives longest_run_hour.
    longest_cycle AS (
        SELECT DISTINCT ON (group_id, device_id, sensor_id)
            group_id,
            device_id,
            sensor_id,
            peak_work_min AS longest_run_minutes,
            EXTRACT(hour FROM peak_hour AT TIME ZONE p_timezone)::integer AS longest_run_hour
        FROM cycles
        ORDER BY group_id, device_id, sensor_id, peak_work_min DESC NULLS LAST
    )
    SELECT
        c.group_id,
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
    JOIN window_bounds wb      USING (group_id, device_id, sensor_id)
    JOIN longest_cycle lc      USING (group_id, device_id, sensor_id)
    JOIN peak_hour_cycling phc USING (group_id, device_id, sensor_id)
    GROUP BY c.group_id, c.device_id, c.sensor_id,
             wb.first_hour, wb.last_hour,
             lc.longest_run_minutes, lc.longest_run_hour,
             phc.peak_cycling_hour, phc.peak_cycling_count;
$function$;
