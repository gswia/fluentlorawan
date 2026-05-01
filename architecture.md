# Architecture

**Working Directory Structure:**
- `OLD/sql/schema/` - Current table definitions (sensor_readings, gateway_readings, config tables)
- `OLD/sql/functions/` - Current continuous aggregates and functions (copied from /sql)
- `NEW/sql/schema/` - Modified table definitions for Group model
- `NEW/sql/functions/` - Modified continuous aggregates and functions for Group model
- `NEW/sql/migrations/` - Data migration scripts

## Phase 1: Create SQL File Definitions

- [ ] 1. Create base hypertable schema files: `sensor_readings` and `gateway_readings` with current structure (including application_id, site_id for now). Define proper indexes, constraints, and TimescaleDB hypertable configuration.

- [ ] 2. Create Group model table definitions: `accounts` (account_id PK, name, storage_config JSONB), `users` (user_id PK, email, name), `roles` (role_id PK, name, permissions text[]), `user_accounts` junction (user_id, account_id, role_id), `groups` (group_id PK, account_id FK, name, group_type, timezone), `devices` (device_id PK, device_profile), `sensors` (sensor_id PK, device_id FK, sensor_type, description), `device_groups` junction (device_id, group_id). Include all foreign keys, indexes, and constraints.

- [ ] 3. Copy continuous aggregate definitions from `/sql` to `NEW/sql/functions/`: `hourly_sensor_temperature_stats`, `hourly_sensor_humidity_stats`, `hourly_sensor_illumination_stats`, `hourly_sensor_door_stats`, `hourly_sensor_vibration_stats`. Keep referencing `sensor_readings` table.

- [ ] 4. Copy window stats functions from `/sql` to `NEW/sql/functions/`: `get_temperature_window_stats`, `get_humidity_window_stats`, `get_illumination_window_stats`, `get_door_window_stats`, `get_vibration_window_stats`. Keep referencing existing continuous aggregates.

- [ ] 5. Copy analysis function `get_timezone_analysis_stats` from `/sql` to `NEW/sql/functions/`. Keep referencing all window functions.

## Phase 2: Modify SQL Files for Group Model

- [ ] 6. Update `sensor_readings` and `gateway_readings` schema: Add `group_id` column, remove `application_id` and `site_id` columns. Update indexes to use `group_id` instead of site_id.

- [ ] 7. Update all window stats functions to eliminate `sensor_site_map` and `sites` table dependencies. Replace `sites_in_tz` CTE with direct `groups` join using `group_id`. Update all JOIN clauses to use `sensors` and `groups` tables.

- [ ] 8. Refactor `get_timezone_analysis_stats` to accept flexible time period parameters instead of hardcoded 24-hour intervals. Add parameters for current and previous window sizes to enable arbitrary period comparisons (24h, week, month).

- [ ] 9. Remove `get_timezone_analysis_stats_enriched` function. Update base analysis function to join sensor descriptions directly from `sensors` table instead of JSONB traversal.

- [ ] 10. Create `ArizonaMigration.sql` to transform existing data from Application→Site hierarchy into Group-based schema. Map current application_id+site_id combinations to new group_id values and populate all junction tables.

