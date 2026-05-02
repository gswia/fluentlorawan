-- Deploy v1 schema for Group-based IoT model
-- Run this script on your TimescaleDB database
-- Isolates new schema from existing public schema

-- Phase 1: Create schema and tables
\i NEW/sql/schema/00_create_schema.sql
\i NEW/sql/schema/00_tables_readings.sql
\i NEW/sql/schema/01_tables_group_model.sql

-- Phase 2: Create continuous aggregates
\i NEW/sql/functions/01_cagg_hourly_sensor_temperature_stats.sql
\i NEW/sql/functions/02_cagg_hourly_sensor_humidity_stats.sql
\i NEW/sql/functions/03_cagg_hourly_sensor_illumination_stats.sql
\i NEW/sql/functions/04_cagg_hourly_sensor_door_stats.sql
\i NEW/sql/functions/05_cagg_hourly_sensor_vibration_stats.sql

-- Phase 3: Create window stats functions
\i NEW/sql/functions/06_fn_get_temperature_window_stats.sql
\i NEW/sql/functions/07_fn_get_humidity_window_stats.sql
\i NEW/sql/functions/08_fn_get_illumination_window_stats.sql
\i NEW/sql/functions/09_fn_get_door_window_stats.sql
\i NEW/sql/functions/10_fn_get_vibration_window_stats.sql

-- Phase 4: Create timezone analysis function
\i NEW/sql/functions/11_fn_get_timezone_analysis_stats.sql
