-- Wraps get_timezone_analysis_stats and injects a human-readable `description`
-- field directly into each sensor object, sourced from the accounts JSONB config.
-- This eliminates the need for a separate sensor-name lookup at query time —
-- the LLM receives self-describing sensor objects with no UUID cross-referencing.
CREATE OR REPLACE FUNCTION get_timezone_analysis_stats_enriched(
    p_timezone          character varying,
    p_analysis_datetime timestamp with time zone)
RETURNS TABLE(out_site_id character varying, analysis_data jsonb)
LANGUAGE sql STABLE AS $function$
WITH sensor_lookup AS (
    SELECT
        s->>'SensorId'    AS sensor_id,
        s->>'Description' AS description
    FROM accounts,
         jsonb_array_elements(account->'Applications') AS app,
         jsonb_array_elements(app->'Sites')            AS site,
         jsonb_array_elements(site->'Devices')         AS dev,
         jsonb_array_elements(dev->'Sensors')          AS s
),
analysis AS (
    SELECT * FROM get_timezone_analysis_stats(p_timezone, p_analysis_datetime)
),
sensors_expanded AS (
    SELECT
        a.out_site_id,
        a.analysis_data - 'sensors'                                                   AS base_data,
        sensor_obj || jsonb_build_object('description', sl.description)               AS enriched_sensor
    FROM analysis a,
         jsonb_array_elements(a.analysis_data->'sensors') AS sensor_obj
    LEFT JOIN sensor_lookup sl ON sl.sensor_id = sensor_obj->>'sensor_id'
),
sensors_reaggregated AS (
    SELECT
        out_site_id,
        (array_agg(base_data))[1]                                                     AS base_data,
        jsonb_agg(
            enriched_sensor
            ORDER BY enriched_sensor->>'sensor_type', enriched_sensor->>'sensor_id'
        )                                                                              AS sensors_arr
    FROM sensors_expanded
    GROUP BY out_site_id
)
SELECT
    out_site_id,
    base_data || jsonb_build_object('sensors', sensors_arr) AS analysis_data
FROM sensors_reaggregated;
$function$;
