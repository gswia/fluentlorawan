-- Test query: Compare last month vs previous month
-- Adjust NOW() to any specific timestamp for testing

WITH time_windows AS (
    SELECT
        date_trunc('hour', NOW()) AS current_end,
        date_trunc('hour', NOW()) - INTERVAL '1 month' AS current_start,
        date_trunc('hour', NOW()) - INTERVAL '1 month' AS previous_end,
        date_trunc('hour', NOW()) - INTERVAL '2 months' AS previous_start
),
temp_current AS (
    SELECT * FROM get_temperature_window_stats(
        'America/Phoenix',
        (SELECT current_start FROM time_windows),
        (SELECT current_end FROM time_windows)
    )
),
temp_previous AS (
    SELECT * FROM get_temperature_window_stats(
        'America/Phoenix',
        (SELECT previous_start FROM time_windows),
        (SELECT previous_end FROM time_windows)
    )
),
hum_current AS (
    SELECT * FROM get_humidity_window_stats(
        'America/Phoenix',
        (SELECT current_start FROM time_windows),
        (SELECT current_end FROM time_windows)
    )
),
hum_previous AS (
    SELECT * FROM get_humidity_window_stats(
        'America/Phoenix',
        (SELECT previous_start FROM time_windows),
        (SELECT previous_end FROM time_windows)
    )
),
illum_current AS (
    SELECT * FROM get_illumination_window_stats(
        'America/Phoenix',
        (SELECT current_start FROM time_windows),
        (SELECT current_end FROM time_windows)
    )
),
illum_previous AS (
    SELECT * FROM get_illumination_window_stats(
        'America/Phoenix',
        (SELECT previous_start FROM time_windows),
        (SELECT previous_end FROM time_windows)
    )
),
door_current AS (
    SELECT * FROM get_door_window_stats(
        'America/Phoenix',
        (SELECT current_start FROM time_windows),
        (SELECT current_end FROM time_windows)
    )
),
door_previous AS (
    SELECT * FROM get_door_window_stats(
        'America/Phoenix',
        (SELECT previous_start FROM time_windows),
        (SELECT previous_end FROM time_windows)
    )
),
vib_current AS (
    SELECT * FROM get_vibration_window_stats(
        'America/Phoenix',
        (SELECT current_start FROM time_windows),
        (SELECT current_end FROM time_windows)
    )
),
vib_previous AS (
    SELECT * FROM get_vibration_window_stats(
        'America/Phoenix',
        (SELECT previous_start FROM time_windows),
        (SELECT previous_end FROM time_windows)
    )
),
all_sensors AS (
    -- Temperature sensors
    SELECT
        tc.site_id,
        tc.device_id,
        tc.sensor_id,
        'Temperature' AS sensor_type,
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
        ) AS current_7d,
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
        ) AS previous_7d,
        jsonb_build_object(
            'min_delta',          tc.min_val      - tp.min_val,
            'avg_delta',          tc.avg_val      - tp.avg_val,
            'max_delta',          tc.max_val      - tp.max_val,
            'median_delta',       tc.median       - tp.median,
            'range_delta',        tc.range_val    - tp.range_val,
            'hottest_hour_shift', tc.hottest_hour - tp.hottest_hour
        ) AS changes
    FROM temp_current tc
    LEFT JOIN temp_previous tp USING (site_id, device_id, sensor_id)

    UNION ALL

    -- Humidity sensors
    SELECT
        hc.site_id,
        hc.device_id,
        hc.sensor_id,
        'Humidity' AS sensor_type,
        jsonb_build_object(
            'window_start',   hc.actual_window_start,
            'window_end',     hc.actual_window_end,
            'min',            hc.min_val,
            'avg',            hc.avg_val,
            'median',         hc.median,
            'max',            hc.max_val,
            'highest_hour',   hc.highest_hour,
            'lowest_hour',    hc.lowest_hour,
            'total_readings', hc.total_readings
        ) AS current_7d,
        jsonb_build_object(
            'window_start',   hp.actual_window_start,
            'window_end',     hp.actual_window_end,
            'min',            hp.min_val,
            'avg',            hp.avg_val,
            'median',         hp.median,
            'max',            hp.max_val,
            'highest_hour',   hp.highest_hour,
            'lowest_hour',    hp.lowest_hour,
            'total_readings', hp.total_readings
        ) AS previous_7d,
        jsonb_build_object(
            'min_delta',          hc.min_val      - hp.min_val,
            'avg_delta',          hc.avg_val      - hp.avg_val,
            'max_delta',          hc.max_val      - hp.max_val,
            'median_delta',       hc.median       - hp.median
        ) AS changes
    FROM hum_current hc
    LEFT JOIN hum_previous hp USING (site_id, device_id, sensor_id)

    UNION ALL

    -- Illumination sensors
    SELECT
        ic.site_id,
        ic.device_id,
        ic.sensor_id,
        'Illumination' AS sensor_type,
        jsonb_build_object(
            'window_start',   ic.actual_window_start,
            'window_end',     ic.actual_window_end,
            'min',            ic.min_val,
            'avg',            ic.avg_val,
            'max',            ic.max_val,
            'brightest_hour', ic.brightest_hour,
            'total_readings', ic.total_readings
        ) AS current_7d,
        jsonb_build_object(
            'window_start',   ip.actual_window_start,
            'window_end',     ip.actual_window_end,
            'min',            ip.min_val,
            'avg',            ip.avg_val,
            'max',            ip.max_val,
            'brightest_hour', ip.brightest_hour,
            'total_readings', ip.total_readings
        ) AS previous_7d,
        jsonb_build_object(
            'avg_delta',          ic.avg_val - ip.avg_val,
            'max_delta',          ic.max_val - ip.max_val
        ) AS changes
    FROM illum_current ic
    LEFT JOIN illum_previous ip USING (site_id, device_id, sensor_id)

    UNION ALL

    -- Door sensors
    SELECT
        dc.site_id,
        dc.device_id,
        dc.sensor_id,
        'Door' AS sensor_type,
        jsonb_build_object(
            'window_start',              dc.actual_window_start,
            'window_end',                dc.actual_window_end,
            'total_open_times',          dc.total_open_times,
            'total_open_duration_minutes', dc.total_open_duration_minutes,
            'longest_open_minutes',      dc.longest_open_minutes,
            'longest_open_hour',         dc.longest_open_hour,
            'avg',                       dc.avg_open_duration,
            'median',                    dc.median_open_duration,
            'total_readings',            dc.total_readings
        ) AS current_7d,
        jsonb_build_object(
            'window_start',              dp.actual_window_start,
            'window_end',                dp.actual_window_end,
            'total_open_times',          dp.total_open_times,
            'total_open_duration_minutes', dp.total_open_duration_minutes,
            'longest_open_minutes',      dp.longest_open_minutes,
            'longest_open_hour',         dp.longest_open_hour,
            'avg',                       dp.avg_open_duration,
            'median',                    dp.median_open_duration,
            'total_readings',            dp.total_readings
        ) AS previous_7d,
        jsonb_build_object(
            'open_times_delta',          dc.total_open_times - dp.total_open_times,
            'total_duration_delta',      dc.total_open_duration_minutes - dp.total_open_duration_minutes,
            'longest_open_delta',        dc.longest_open_minutes - dp.longest_open_minutes
        ) AS changes
    FROM door_current dc
    LEFT JOIN door_previous dp USING (site_id, device_id, sensor_id)

    UNION ALL

    -- Vibration sensors (HVAC)
    SELECT
        vc.site_id,
        vc.device_id,
        vc.sensor_id,
        'Vibration' AS sensor_type,
        jsonb_build_object(
            'window_start',         vc.actual_window_start,
            'window_end',           vc.actual_window_end,
            'total_cycles',         vc.total_cycles,
            'total_run_minutes',    vc.total_run_minutes,
            'avg_run_minutes',      vc.avg_run_minutes,
            'longest_run_minutes',  vc.longest_run_minutes,
            'avg_off_minutes',      vc.avg_off_minutes,
            'peak_cycling_hour',    vc.peak_cycling_hour,
            'total_readings',       vc.total_readings
        ) AS current_7d,
        jsonb_build_object(
            'window_start',         vp.actual_window_start,
            'window_end',           vp.actual_window_end,
            'total_cycles',         vp.total_cycles,
            'total_run_minutes',    vp.total_run_minutes,
            'avg_run_minutes',      vp.avg_run_minutes,
            'longest_run_minutes',  vp.longest_run_minutes,
            'avg_off_minutes',      vp.avg_off_minutes,
            'peak_cycling_hour',    vp.peak_cycling_hour,
            'total_readings',       vp.total_readings
        ) AS previous_7d,
        jsonb_build_object(
            'total_cycles_delta',       vc.total_cycles - vp.total_cycles,
            'total_run_minutes_delta',  vc.total_run_minutes - vp.total_run_minutes,
            'avg_run_minutes_delta',    vc.avg_run_minutes - vp.avg_run_minutes
        ) AS changes
    FROM vib_current vc
    LEFT JOIN vib_previous vp USING (site_id, device_id, sensor_id)
),
sensor_lookup AS (
    SELECT
        s->>'SensorId'    AS sensor_id,
        s->>'Description' AS description
    FROM accounts,
         jsonb_array_elements(account->'Applications') AS app,
         jsonb_array_elements(app->'Sites')            AS site,
         jsonb_array_elements(site->'Devices')         AS dev,
         jsonb_array_elements(dev->'Sensors')          AS s
)
SELECT
    a.site_id,
    jsonb_build_object(
        'timezone', 'America/Phoenix',
        'analysis_type', 'monthly_comparison',
        'sensors', jsonb_agg(
            jsonb_build_object(
                'sensor_id', a.sensor_id,
                'sensor_type', a.sensor_type,
                'description', sl.description,
                'current_month', a.current_7d,
                'previous_month', a.previous_7d,
                'changes', a.changes
            )
            ORDER BY a.sensor_type, a.sensor_id
        )
    ) AS analysis_data
FROM all_sensors a
LEFT JOIN sensor_lookup sl ON sl.sensor_id = a.sensor_id
GROUP BY a.site_id;
