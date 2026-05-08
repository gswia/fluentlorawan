-- AI Agent MCP Function: Flexible sensor analysis with optional filtering and comparison
-- 
-- Parameter Summary:
-- - p_account_id: REQUIRED - Account scope for all queries
-- - p_timezone: REQUIRED - Timezone for analysis (e.g., 'America/New_York')
-- - p_analysis_datetime: REQUIRED - Reference time for window calculation
-- - p_current_window: REQUIRED - Duration of current analysis window (e.g., '24 hours')
-- - p_group_id: OPTIONAL - Filter to specific group (NULL = all groups in account)
-- - p_sensor_types: OPTIONAL - Array of sensor types to include (NULL = all types)
--   Examples: ARRAY['Temperature'], ARRAY['Temperature','Humidity'], NULL
-- - p_previous_window: OPTIONAL - Duration of comparison window (NULL = no comparison, skip changes)
--
-- Behavior Changes:
-- 1. When p_previous_window IS NULL:
--    - Skips all previous_* CTEs (performance optimization)
--    - Returns NULL for 'previous_window' and 'changes' fields
--    - ~50% faster execution for diagnostic queries without comparison needs
-- 2. When p_group_id IS NULL:
--    - Returns data for ALL groups in the account
-- 3. When p_sensor_types IS NULL:
--    - Returns data for ALL sensor types (Temperature, Humidity, Door, Vibration, Illumination)
-- 4. Includes metadata enrichment:
--    - sensor_profile->>'measures': What the sensor measures (e.g., "Bedroom 2 Temperature")
--    - group_name: Human-readable group name for agent context

CREATE OR REPLACE FUNCTION v1.get_timezone_analysis_stats(
    p_account_id UUID,
    p_timezone TEXT,
    p_analysis_datetime TIMESTAMPTZ,
    p_current_window INTERVAL,
    p_group_id UUID DEFAULT NULL,
    p_sensor_types TEXT[] DEFAULT NULL,
    p_previous_window INTERVAL DEFAULT NULL
)
RETURNS TABLE(out_group_id UUID, analysis_data JSONB)
LANGUAGE plpgsql
STABLE
AS $function$
DECLARE
    v_window_start   TIMESTAMPTZ;
    v_window_end     TIMESTAMPTZ;
    v_previous_start TIMESTAMPTZ;
    v_previous_end   TIMESTAMPTZ;
    v_include_temperature BOOLEAN;
    v_include_humidity BOOLEAN;
    v_include_illumination BOOLEAN;
    v_include_door BOOLEAN;
    v_include_vibration BOOLEAN;
    v_compare_mode BOOLEAN;
BEGIN
    -- Calculate time windows
    v_window_end     := date_trunc('hour', p_analysis_datetime);
    v_window_start   := v_window_end - p_current_window;
    
    -- Determine comparison mode
    v_compare_mode := (p_previous_window IS NOT NULL);
    
    IF v_compare_mode THEN
        v_previous_end   := v_window_start;
        v_previous_start := v_previous_end - p_previous_window;
    END IF;
    
    -- Determine which sensor types to include
    v_include_temperature := (p_sensor_types IS NULL OR 'Temperature' = ANY(p_sensor_types));
    v_include_humidity := (p_sensor_types IS NULL OR 'Humidity' = ANY(p_sensor_types));
    v_include_illumination := (p_sensor_types IS NULL OR 'Illumination' = ANY(p_sensor_types));
    v_include_door := (p_sensor_types IS NULL OR 'Door' = ANY(p_sensor_types));
    v_include_vibration := (p_sensor_types IS NULL OR 'Vibration' = ANY(p_sensor_types));

    RETURN QUERY
    WITH 
    -- Filter groups by account_id and optional group_id
    filtered_groups AS (
        SELECT g.group_id, g.name AS group_name, g.timezone
        FROM v1.groups g
        WHERE g.account_id = p_account_id
          AND g.timezone = p_timezone
          AND (p_group_id IS NULL OR g.group_id = p_group_id)
    ),
    -- Current window CTEs (always executed)
    temp_current AS (
        SELECT tw.*
        FROM v1.get_temperature_window_stats(p_timezone, v_window_start, v_window_end) tw
        WHERE v_include_temperature
          AND tw.group_id IN (SELECT group_id FROM filtered_groups)
    ),
    hum_current AS (
        SELECT hw.*
        FROM v1.get_humidity_window_stats(p_timezone, v_window_start, v_window_end) hw
        WHERE v_include_humidity
          AND hw.group_id IN (SELECT group_id FROM filtered_groups)
    ),
    illum_current AS (
        SELECT iw.*
        FROM v1.get_illumination_window_stats(p_timezone, v_window_start, v_window_end) iw
        WHERE v_include_illumination
          AND iw.group_id IN (SELECT group_id FROM filtered_groups)
    ),
    door_current AS (
        SELECT dw.*
        FROM v1.get_door_window_stats(p_timezone, v_window_start, v_window_end) dw
        WHERE v_include_door
          AND dw.group_id IN (SELECT group_id FROM filtered_groups)
    ),
    vib_current AS (
        SELECT vw.*
        FROM v1.get_vibration_window_stats(p_timezone, v_window_start, v_window_end) vw
        WHERE v_include_vibration
          AND vw.group_id IN (SELECT group_id FROM filtered_groups)
    ),
    -- Previous window CTEs (only executed if v_compare_mode = true)
    temp_previous AS (
        SELECT tw.*
        FROM v1.get_temperature_window_stats(p_timezone, v_previous_start, v_previous_end) tw
        WHERE v_compare_mode AND v_include_temperature
          AND tw.group_id IN (SELECT group_id FROM filtered_groups)
    ),
    hum_previous AS (
        SELECT hw.*
        FROM v1.get_humidity_window_stats(p_timezone, v_previous_start, v_previous_end) hw
        WHERE v_compare_mode AND v_include_humidity
          AND hw.group_id IN (SELECT group_id FROM filtered_groups)
    ),
    illum_previous AS (
        SELECT iw.*
        FROM v1.get_illumination_window_stats(p_timezone, v_previous_start, v_previous_end) iw
        WHERE v_compare_mode AND v_include_illumination
          AND iw.group_id IN (SELECT group_id FROM filtered_groups)
    ),
    door_previous AS (
        SELECT dw.*
        FROM v1.get_door_window_stats(p_timezone, v_previous_start, v_previous_end) dw
        WHERE v_compare_mode AND v_include_door
          AND dw.group_id IN (SELECT group_id FROM filtered_groups)
    ),
    vib_previous AS (
        SELECT vw.*
        FROM v1.get_vibration_window_stats(p_timezone, v_previous_start, v_previous_end) vw
        WHERE v_compare_mode AND v_include_vibration
          AND vw.group_id IN (SELECT group_id FROM filtered_groups)
    ),
    -- Combine all sensor types with metadata enrichment
    all_sensors AS (
        -- Temperature sensors
        SELECT
            tc.group_id,
            tc.device_id,
            tc.sensor_id,
            'Temperature' AS sensor_type,
            s.sensor_profile->>'measures' AS measures,
            jsonb_build_object(
                'window_start',   tc.actual_window_start,
                'window_end',     tc.actual_window_end,
                'min',            tc.min_val,
                'p10',            tc.p10,
                'p25',            tc.p25,
                'median',         tc.median,
                'avg',            tc.avg_val,
                'p75',            tc.p75,
                'p90',            tc.p90,
                'max',            tc.max_val,
                'stddev',         tc.stddev_val,
                'iqr',            tc.iqr,
                'range',          tc.range_val,
                'hottest_hour',   tc.hottest_hour,
                'coldest_hour',   tc.coldest_hour,
                'total_readings', tc.total_readings
            ) AS current_window,
            CASE WHEN v_compare_mode THEN
                jsonb_build_object(
                    'window_start',   tp.actual_window_start,
                    'window_end',     tp.actual_window_end,
                    'min',            tp.min_val,
                    'p10',            tp.p10,
                    'p25',            tp.p25,
                    'median',         tp.median,
                    'avg',            tp.avg_val,
                    'p75',            tp.p75,
                    'p90',            tp.p90,
                    'max',            tp.max_val,
                    'stddev',         tp.stddev_val,
                    'iqr',            tp.iqr,
                    'range',          tp.range_val,
                    'hottest_hour',   tp.hottest_hour,
                    'coldest_hour',   tp.coldest_hour,
                    'total_readings', tp.total_readings
                )
            ELSE NULL END AS previous_window,
            CASE WHEN v_compare_mode THEN
                jsonb_build_object(
                    'min_delta',          tc.min_val      - tp.min_val,
                    'p10_delta',          tc.p10          - tp.p10,
                    'p25_delta',          tc.p25          - tp.p25,
                    'median_delta',       tc.median       - tp.median,
                    'avg_delta',          tc.avg_val      - tp.avg_val,
                    'p75_delta',          tc.p75          - tp.p75,
                    'p90_delta',          tc.p90          - tp.p90,
                    'max_delta',          tc.max_val      - tp.max_val,
                    'stddev_delta',       tc.stddev_val   - tp.stddev_val,
                    'iqr_delta',          tc.iqr          - tp.iqr,
                    'range_delta',        tc.range_val    - tp.range_val,
                    'hottest_hour_shift', tc.hottest_hour - tp.hottest_hour,
                    'coldest_hour_shift', tc.coldest_hour - tp.coldest_hour
                )
            ELSE NULL END AS changes
        FROM temp_current tc
        LEFT JOIN temp_previous tp ON v_compare_mode AND tp.group_id = tc.group_id AND tp.device_id = tc.device_id AND tp.sensor_id = tc.sensor_id
        LEFT JOIN v1.sensors s ON s.sensor_id = tc.sensor_id

        UNION ALL

        -- Humidity sensors
        SELECT
            hc.group_id,
            hc.device_id,
            hc.sensor_id,
            'Humidity' AS sensor_type,
            s.sensor_profile->>'measures' AS measures,
            jsonb_build_object(
                'window_start',   hc.actual_window_start,
                'window_end',     hc.actual_window_end,
                'min',            hc.min_val,
                'p10',            hc.p10,
                'p25',            hc.p25,
                'median',         hc.median,
                'avg',            hc.avg_val,
                'p75',            hc.p75,
                'p90',            hc.p90,
                'max',            hc.max_val,
                'stddev',         hc.stddev_val,
                'iqr',            hc.iqr,
                'range',          hc.range_val,
                'highest_hour',   hc.highest_hour,
                'lowest_hour',    hc.lowest_hour,
                'total_readings', hc.total_readings
            ) AS current_window,
            CASE WHEN v_compare_mode THEN
                jsonb_build_object(
                    'window_start',   hp.actual_window_start,
                    'window_end',     hp.actual_window_end,
                    'min',            hp.min_val,
                    'p10',            hp.p10,
                    'p25',            hp.p25,
                    'median',         hp.median,
                    'avg',            hp.avg_val,
                    'p75',            hp.p75,
                    'p90',            hp.p90,
                    'max',            hp.max_val,
                    'stddev',         hp.stddev_val,
                    'iqr',            hp.iqr,
                    'range',          hp.range_val,
                    'highest_hour',   hp.highest_hour,
                    'lowest_hour',    hp.lowest_hour,
                    'total_readings', hp.total_readings
                )
            ELSE NULL END AS previous_window,
            CASE WHEN v_compare_mode THEN
                jsonb_build_object(
                    'min_delta',          hc.min_val      - hp.min_val,
                    'p10_delta',          hc.p10          - hp.p10,
                    'p25_delta',          hc.p25          - hp.p25,
                    'median_delta',       hc.median       - hp.median,
                    'avg_delta',          hc.avg_val      - hp.avg_val,
                    'p75_delta',          hc.p75          - hp.p75,
                    'p90_delta',          hc.p90          - hp.p90,
                    'max_delta',          hc.max_val      - hp.max_val,
                    'stddev_delta',       hc.stddev_val   - hp.stddev_val,
                    'iqr_delta',          hc.iqr          - hp.iqr,
                    'range_delta',        hc.range_val    - hp.range_val,
                    'highest_hour_shift', hc.highest_hour - hp.highest_hour,
                    'lowest_hour_shift',  hc.lowest_hour  - hp.lowest_hour
                )
            ELSE NULL END AS changes
        FROM hum_current hc
        LEFT JOIN hum_previous hp ON v_compare_mode AND hp.group_id = hc.group_id AND hp.device_id = hc.device_id AND hp.sensor_id = hc.sensor_id
        LEFT JOIN v1.sensors s ON s.sensor_id = hc.sensor_id

        UNION ALL

        -- Illumination sensors
        SELECT
            ic.group_id,
            ic.device_id,
            ic.sensor_id,
            'Illumination' AS sensor_type,
            s.sensor_profile->>'measures' AS measures,
            jsonb_build_object(
                'window_start',   ic.actual_window_start,
                'window_end',     ic.actual_window_end,
                'min',            ic.min_val,
                'p10',            ic.p10,
                'p25',            ic.p25,
                'median',         ic.median,
                'avg',            ic.avg_val,
                'p75',            ic.p75,
                'p90',            ic.p90,
                'max',            ic.max_val,
                'stddev',         ic.stddev_val,
                'iqr',            ic.iqr,
                'range',          ic.range_val,
                'brightest_hour', ic.brightest_hour,
                'darkest_hour',   ic.darkest_hour,
                'total_readings', ic.total_readings
            ) AS current_window,
            CASE WHEN v_compare_mode THEN
                jsonb_build_object(
                    'window_start',   ip.actual_window_start,
                    'window_end',     ip.actual_window_end,
                    'min',            ip.min_val,
                    'p10',            ip.p10,
                    'p25',            ip.p25,
                    'median',         ip.median,
                    'avg',            ip.avg_val,
                    'p75',            ip.p75,
                    'p90',            ip.p90,
                    'max',            ip.max_val,
                    'stddev',         ip.stddev_val,
                    'iqr',            ip.iqr,
                    'range',          ip.range_val,
                    'brightest_hour', ip.brightest_hour,
                    'darkest_hour',   ip.darkest_hour,
                    'total_readings', ip.total_readings
                )
            ELSE NULL END AS previous_window,
            CASE WHEN v_compare_mode THEN
                jsonb_build_object(
                    'min_delta',            ic.min_val        - ip.min_val,
                    'p10_delta',            ic.p10            - ip.p10,
                    'p25_delta',            ic.p25            - ip.p25,
                    'median_delta',         ic.median         - ip.median,
                    'avg_delta',            ic.avg_val        - ip.avg_val,
                    'p75_delta',            ic.p75            - ip.p75,
                    'p90_delta',            ic.p90            - ip.p90,
                    'max_delta',            ic.max_val        - ip.max_val,
                    'stddev_delta',         ic.stddev_val     - ip.stddev_val,
                    'iqr_delta',            ic.iqr            - ip.iqr,
                    'range_delta',          ic.range_val      - ip.range_val,
                    'brightest_hour_shift', ic.brightest_hour - ip.brightest_hour,
                    'darkest_hour_shift',   ic.darkest_hour   - ip.darkest_hour
                )
            ELSE NULL END AS changes
        FROM illum_current ic
        LEFT JOIN illum_previous ip ON v_compare_mode AND ip.group_id = ic.group_id AND ip.device_id = ic.device_id AND ip.sensor_id = ic.sensor_id
        LEFT JOIN v1.sensors s ON s.sensor_id = ic.sensor_id

        UNION ALL

        -- Door sensors
        SELECT
            dc.group_id,
            dc.device_id,
            dc.sensor_id,
            'Door' AS sensor_type,
            s.sensor_profile->>'measures' AS measures,
            jsonb_build_object(
                'window_start',                dc.actual_window_start,
                'window_end',                  dc.actual_window_end,
                'total_alarms',                dc.total_alarms,
                'total_open_times',            dc.total_open_times,
                'total_open_duration_minutes', dc.total_open_duration_minutes,
                'longest_open_minutes',        dc.longest_open_minutes,
                'longest_open_hour',           dc.longest_open_hour,
                'avg',                         dc.avg_open_duration,
                'median',                      dc.median_open_duration,
                'stddev',                      dc.stddev_open_duration,
                'p75',                         dc.p75_open_duration,
                'p90',                         dc.p90_open_duration,
                'iqr',                         dc.iqr_open_duration,
                'hours_with_open',             dc.hours_with_open,
                'total_readings',              dc.total_readings
            ) AS current_window,
            CASE WHEN v_compare_mode THEN
                jsonb_build_object(
                    'window_start',                dp.actual_window_start,
                    'window_end',                  dp.actual_window_end,
                    'total_alarms',                dp.total_alarms,
                    'total_open_times',            dp.total_open_times,
                    'total_open_duration_minutes', dp.total_open_duration_minutes,
                    'longest_open_minutes',        dp.longest_open_minutes,
                    'longest_open_hour',           dp.longest_open_hour,
                    'avg',                         dp.avg_open_duration,
                    'median',                      dp.median_open_duration,
                    'stddev',                      dp.stddev_open_duration,
                    'p75',                         dp.p75_open_duration,
                    'p90',                         dp.p90_open_duration,
                    'iqr',                         dp.iqr_open_duration,
                    'hours_with_open',             dp.hours_with_open,
                    'total_readings',              dp.total_readings
                )
            ELSE NULL END AS previous_window,
            CASE WHEN v_compare_mode THEN
                jsonb_build_object(
                    'alarm_delta',           dc.total_alarms                - dp.total_alarms,
                    'open_times_delta',      dc.total_open_times            - dp.total_open_times,
                    'open_duration_delta',   dc.total_open_duration_minutes - dp.total_open_duration_minutes,
                    'longest_open_delta',    dc.longest_open_minutes        - dp.longest_open_minutes,
                    'avg_delta',             dc.avg_open_duration           - dp.avg_open_duration,
                    'median_delta',          dc.median_open_duration        - dp.median_open_duration,
                    'stddev_delta',          dc.stddev_open_duration        - dp.stddev_open_duration,
                    'p75_delta',             dc.p75_open_duration           - dp.p75_open_duration,
                    'p90_delta',             dc.p90_open_duration           - dp.p90_open_duration,
                    'iqr_delta',             dc.iqr_open_duration           - dp.iqr_open_duration,
                    'hours_with_open_delta', dc.hours_with_open             - dp.hours_with_open
                )
            ELSE NULL END AS changes
        FROM door_current dc
        LEFT JOIN door_previous dp ON v_compare_mode AND dp.group_id = dc.group_id AND dp.device_id = dc.device_id AND dp.sensor_id = dc.sensor_id
        LEFT JOIN v1.sensors s ON s.sensor_id = dc.sensor_id

        UNION ALL

        -- Vibration sensors
        SELECT
            vc.group_id,
            vc.device_id,
            vc.sensor_id,
            'Vibration' AS sensor_type,
            s.sensor_profile->>'measures' AS measures,
            jsonb_build_object(
                'window_start',        vc.actual_window_start,
                'window_end',          vc.actual_window_end,
                'total_cycles',        vc.total_cycles,
                'total_run_minutes',   vc.total_run_minutes,
                'avg_run_minutes',     vc.avg_run_minutes,
                'median_run_minutes',  vc.median_run_minutes,
                'p75_run_minutes',     vc.p75_run_minutes,
                'p90_run_minutes',     vc.p90_run_minutes,
                'longest_run_minutes', vc.longest_run_minutes,
                'longest_run_hour',    vc.longest_run_hour,
                'avg_off_minutes',     vc.avg_off_minutes,
                'peak_cycling_hour',   vc.peak_cycling_hour,
                'peak_cycling_count',  vc.peak_cycling_count,
                'total_alarms',        vc.total_alarms,
                'total_readings',      vc.total_readings
            ) AS current_window,
            CASE WHEN v_compare_mode THEN
                jsonb_build_object(
                    'window_start',        vp.actual_window_start,
                    'window_end',          vp.actual_window_end,
                    'total_cycles',        vp.total_cycles,
                    'total_run_minutes',   vp.total_run_minutes,
                    'avg_run_minutes',     vp.avg_run_minutes,
                    'median_run_minutes',  vp.median_run_minutes,
                    'p75_run_minutes',     vp.p75_run_minutes,
                    'p90_run_minutes',     vp.p90_run_minutes,
                    'longest_run_minutes', vp.longest_run_minutes,
                    'longest_run_hour',    vp.longest_run_hour,
                    'avg_off_minutes',     vp.avg_off_minutes,
                    'peak_cycling_hour',   vp.peak_cycling_hour,
                    'peak_cycling_count',  vp.peak_cycling_count,
                    'total_alarms',        vp.total_alarms,
                    'total_readings',      vp.total_readings
                )
            ELSE NULL END AS previous_window,
            CASE WHEN v_compare_mode THEN
                jsonb_build_object(
                    'total_cycles_delta',         vc.total_cycles        - vp.total_cycles,
                    'total_run_minutes_delta',    vc.total_run_minutes   - vp.total_run_minutes,
                    'avg_run_minutes_delta',      vc.avg_run_minutes     - vp.avg_run_minutes,
                    'median_run_minutes_delta',   vc.median_run_minutes  - vp.median_run_minutes,
                    'p75_run_minutes_delta',      vc.p75_run_minutes     - vp.p75_run_minutes,
                    'p90_run_minutes_delta',      vc.p90_run_minutes     - vp.p90_run_minutes,
                    'longest_run_minutes_delta',  vc.longest_run_minutes - vp.longest_run_minutes,
                    'longest_run_hour_shift',     vc.longest_run_hour    - vp.longest_run_hour,
                    'avg_off_minutes_delta',      vc.avg_off_minutes     - vp.avg_off_minutes,
                    'peak_cycling_count_delta',   vc.peak_cycling_count  - vp.peak_cycling_count,
                    'peak_cycling_hour_shift',    vc.peak_cycling_hour   - vp.peak_cycling_hour,
                    'alarm_delta',                vc.total_alarms        - vp.total_alarms
                )
            ELSE NULL END AS changes
        FROM vib_current vc
        LEFT JOIN vib_previous vp ON v_compare_mode AND vp.group_id = vc.group_id AND vp.device_id = vc.device_id AND vp.sensor_id = vc.sensor_id
        LEFT JOIN v1.sensors s ON s.sensor_id = vc.sensor_id
    ),
    sensors_json AS (
        SELECT
            asens.group_id,
            jsonb_agg(
                jsonb_build_object(
                    'sensor_id',       asens.sensor_id,
                    'device_id',       asens.device_id,
                    'sensor_type',     asens.sensor_type,
                    'measures',        asens.measures,
                    'current_window',  asens.current_window,
                    'previous_window', asens.previous_window,
                    'changes',         asens.changes
                ) ORDER BY asens.device_id, asens.sensor_type
            ) AS sensors
        FROM all_sensors asens
        GROUP BY asens.group_id
    )
    SELECT
        sj.group_id,
        jsonb_build_object(
            'group_id',              sj.group_id,
            'group_name',            fg.group_name,
            'timezone',              p_timezone,
            'analysis_requested_at', p_analysis_datetime AT TIME ZONE p_timezone,
            'requested_period',      jsonb_build_object(
                'current_window',  p_current_window::TEXT,
                'current_start',   v_window_start AT TIME ZONE p_timezone,
                'current_end',     v_window_end AT TIME ZONE p_timezone,
                'previous_window', CASE WHEN v_compare_mode THEN p_previous_window::TEXT ELSE NULL END,
                'previous_start',  CASE WHEN v_compare_mode THEN v_previous_start AT TIME ZONE p_timezone ELSE NULL END,
                'previous_end',    CASE WHEN v_compare_mode THEN v_previous_end AT TIME ZONE p_timezone ELSE NULL END
            ),
            'sensors', sj.sensors
        ) AS analysis_data
    FROM sensors_json sj
    JOIN filtered_groups fg ON fg.group_id = sj.group_id;
END;
$function$;
