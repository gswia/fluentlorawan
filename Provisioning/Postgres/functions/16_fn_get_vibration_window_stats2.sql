-- Graph RAG version: Direct sensor ID filtering (no group/timezone filtering)
CREATE OR REPLACE FUNCTION v1.get_vibration_window_stats2(
    p_sensor_ids   UUID[],
    p_window_start TIMESTAMPTZ,
    p_window_end   TIMESTAMPTZ)
RETURNS TABLE(
    sensor_id           UUID,
    device_id           TEXT,
    actual_window_start TIMESTAMPTZ,
    actual_window_end   TIMESTAMPTZ,
    total_cycles        BIGINT,
    total_run_minutes   BIGINT,
    avg_run_minutes     NUMERIC,
    median_run_minutes  NUMERIC,
    p75_run_minutes     NUMERIC,
    p90_run_minutes     NUMERIC,
    longest_run_minutes INTEGER,
    longest_run_hour    INTEGER,
    avg_off_minutes     NUMERIC,
    peak_cycling_hour   INTEGER,
    peak_cycling_count  INTEGER,
    total_alarms        BIGINT,
    total_readings      BIGINT)
LANGUAGE sql STABLE AS $function$
    WITH vib_window AS (
        SELECT
            s.device_id,
            v.sensor_id,
            v.hour,
            v.vib_count,
            v.max_work_min,
            v.alarm_count,
            v.reading_count
        FROM v1.hourly_sensor_vibration_stats v
        JOIN v1.sensors s ON s.sensor_id = v.sensor_id
        WHERE v.sensor_id = ANY(p_sensor_ids)
          AND v.hour >= p_window_start
          AND v.hour <  p_window_end
    ),
    cycle_peaks AS (
        SELECT DISTINCT ON (device_id, sensor_id, vib_count)
            device_id,
            sensor_id,
            vib_count,
            max_work_min AS peak_work_min,
            hour AS peak_hour
        FROM vib_window
        ORDER BY device_id, sensor_id, vib_count, max_work_min DESC NULLS LAST
    ),
    cycle_totals AS (
        SELECT
            device_id,
            sensor_id,
            vib_count,
            SUM(alarm_count) AS cycle_alarm_count,
            SUM(reading_count) AS cycle_reading_count
        FROM vib_window
        WHERE vib_count IS NOT NULL
        GROUP BY device_id, sensor_id, vib_count
    ),
    cycle_summary AS (
        SELECT
            cp.device_id,
            cp.sensor_id,
            cp.vib_count,
            cp.peak_work_min,
            cp.peak_hour,
            ct.cycle_alarm_count,
            ct.cycle_reading_count
        FROM cycle_peaks cp
        LEFT JOIN cycle_totals ct
          ON cp.device_id = ct.device_id
         AND cp.sensor_id = ct.sensor_id
         AND cp.vib_count = ct.vib_count
    ),
    cycle_stats AS (
        SELECT
            device_id,
            sensor_id,
            COUNT(*) AS total_cycles,
            SUM(peak_work_min) AS total_run_minutes,
            percentile_agg(peak_work_min) AS run_duration_percentiles,
            SUM(cycle_alarm_count) AS total_cycle_alarms,
            SUM(cycle_reading_count) AS total_cycle_readings
        FROM cycle_summary
        GROUP BY device_id, sensor_id
    ),
    longest_run AS (
        SELECT DISTINCT ON (device_id, sensor_id)
            device_id,
            sensor_id,
            peak_work_min AS longest_run_minutes,
            EXTRACT(hour FROM peak_hour)::INTEGER AS longest_run_hour
        FROM cycle_summary
        ORDER BY device_id, sensor_id, peak_work_min DESC NULLS LAST
    ),
    peak_cycling AS (
        SELECT DISTINCT ON (device_id, sensor_id)
            device_id,
            sensor_id,
            EXTRACT(hour FROM peak_hour)::INTEGER AS peak_cycling_hour,
            COUNT(*) AS peak_cycling_count
        FROM cycle_summary
        GROUP BY device_id, sensor_id, EXTRACT(hour FROM peak_hour)
        ORDER BY device_id, sensor_id, COUNT(*) DESC
    )
    SELECT
        cs.sensor_id,
        cs.device_id,
        (SELECT MIN(hour) FROM vib_window WHERE sensor_id = cs.sensor_id) AS actual_window_start,
        (SELECT MAX(hour) + INTERVAL '1 hour' FROM vib_window WHERE sensor_id = cs.sensor_id) AS actual_window_end,
        cs.total_cycles,
        cs.total_run_minutes,
        ROUND(cs.total_run_minutes::NUMERIC / NULLIF(cs.total_cycles, 0), 2) AS avg_run_minutes,
        ROUND(approx_percentile(0.50, cs.run_duration_percentiles)::NUMERIC, 2) AS median_run_minutes,
        ROUND(approx_percentile(0.75, cs.run_duration_percentiles)::NUMERIC, 2) AS p75_run_minutes,
        ROUND(approx_percentile(0.90, cs.run_duration_percentiles)::NUMERIC, 2) AS p90_run_minutes,
        lr.longest_run_minutes,
        lr.longest_run_hour,
        ROUND((EXTRACT(epoch FROM (p_window_end - p_window_start)) / 60.0 - cs.total_run_minutes)::NUMERIC / NULLIF(cs.total_cycles, 0), 2) AS avg_off_minutes,
        pc.peak_cycling_hour,
        pc.peak_cycling_count::INTEGER,
        cs.total_cycle_alarms,
        cs.total_cycle_readings
    FROM cycle_stats cs
    LEFT JOIN longest_run lr ON cs.device_id = lr.device_id AND cs.sensor_id = lr.sensor_id
    LEFT JOIN peak_cycling pc ON cs.device_id = pc.device_id AND cs.sensor_id = pc.sensor_id;
$function$;
