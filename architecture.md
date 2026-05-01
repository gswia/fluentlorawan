# Architecture

**Working Directory Structure:**
- `OLD/sql/schema/` - Current table definitions (sensor_readings, gateway_readings, config tables)
- `OLD/sql/functions/` - Current continuous aggregates and functions (copied from /sql)
- `NEW/sql/schema/` - Modified table definitions for Group model
- `NEW/sql/functions/` - Modified continuous aggregates and functions for Group model
- `NEW/sql/migrations/` - Data migration scripts

## Phase 1: Create NEW Schema Definitions

- [ ] 1. Create `NEW/sql/schema/00_tables_readings.sql`: Define `sensor_readings` and `gateway_readings` hypertables with **group_id** (replacing application_id/site_id). 
  - **sensor_readings columns:** timestamp_utc, account_id, group_id, device_id, sensor_id, message_id, type, payload (JSONB)
  - **Primary Key:** (timestamp_utc, sensor_id) - hypertable partitioning
  - **Indexes (mirroring OLD pattern exactly):**
    - (account_id, timestamp_utc DESC) - **preserve from OLD** for account-level time-range queries
    - (group_id, timestamp_utc DESC) - **replaces (site_id, timestamp_utc)** for group-level time-range queries
    - (device_id, timestamp_utc DESC) - **preserve from OLD** for device-level time-range queries
    - (sensor_id, timestamp_utc DESC) - **preserve from OLD** for sensor-level time-range queries (PK doesn't cover sensor→time efficiently)
    - (type) - **preserve from OLD, CRITICAL** for continuous aggregate WHERE filters
  - **gateway_readings:** Similar structure and index pattern
  - **Note:** All 5 indexes from OLD are preserved with same pattern, only replacing implicit site_id references with group_id

- [ ] 2. Create `NEW/sql/schema/01_tables_group_model.sql`: Define Group model tables with indexes matching OLD query patterns.
  - **accounts:** account_id PK, name, storage_config JSONB
  - **users:** user_id PK, email, name
  - **roles:** role_id PK, name, permissions text[]
  - **user_accounts:** (user_id, account_id, role_id) - compound PK covers all queries
  - **groups:** group_id PK, account_id FK, name, group_type, timezone
    - **Index:** (timezone) - **CRITICAL** for window function filtering `WHERE timezone = p_timezone` (mirrors OLD sites table pattern)
  - **devices:** device_id PK, device_profile
  - **sensors:** sensor_id PK, device_id FK, sensor_type, description
    - **Index:** (device_id) - auto-created from FK, needed for device_groups joins
  - **device_groups:** (device_id, group_id) - many-to-many junction (replaces OLD sensor_site_map)
    - **Primary Key:** (device_id, group_id) - efficiently handles "device → groups" lookups
    - **Index:** (group_id) - **CRITICAL** for "group → devices" reverse lookups (mirrors sensor_site_map pattern: `WHERE site_id IN (...)`)
    - **Note:** OLD sensor_site_map supported `JOIN ON sensor_id WHERE site_id IN (...)`. NEW equivalent: `JOIN sensors ON sensor_id → JOIN device_groups ON device_id WHERE group_id IN (...)`

## Phase 2: Create NEW Functions (Modified from OLD)

- [ ] 3. Create `NEW/sql/functions/01-05_cagg_*.sql`: Copy 5 continuous aggregate definitions from `OLD/sql/functions/` (no modifications needed - they only reference sensor_readings.type and sensor_id which remain unchanged).

- [ ] 4. Create `NEW/sql/functions/05-08,12_fn_get_*_window_stats.sql`: Modify window stats functions from `OLD/sql/functions/`:
  - **OLD join pattern:** `cagg JOIN sensor_site_map sm ON sm.sensor_id = cagg.sensor_id WHERE sm.site_id IN (SELECT site_id FROM sites WHERE timezone = p_timezone)`
  - **NEW join pattern (option A - filter groups first, more efficient):**
    ```sql
    WITH groups_in_tz AS (
        SELECT group_id FROM groups WHERE timezone = p_timezone
    )
    SELECT ...
    FROM hourly_cagg cagg
    JOIN sensors s ON s.sensor_id = cagg.sensor_id
    JOIN device_groups dg ON dg.device_id = s.device_id
    WHERE dg.group_id IN (SELECT group_id FROM groups_in_tz)
      AND cagg.hour >= p_window_start AND cagg.hour < p_window_end
    ```
  - **Return columns:** Replace site_id with group_id in function signature
  - **Continuous aggregates remain:** GROUP BY (hour, sensor_id) with NO group_id - devices can move between groups, want stable historical data

- [ ] 5. Create `NEW/sql/functions/09_fn_get_timezone_analysis_stats.sql`: Modify from `OLD/sql/functions/`:
  - **Add parameters:** p_current_window_hours INTEGER, p_previous_window_hours INTEGER (default both to 24)
  - **Replace:** Hardcoded `INTERVAL '24 hours'` with `INTERVAL '1 hour' * p_current_window_hours`
  - **Optional:** Add sensor descriptions by joining `sensors` table in final output (or leave for API layer)

- [ ] 6. Skip `get_timezone_analysis_stats_enriched` - handle enrichment in base function or API layer.

## Phase 3: Create Migration

- [ ] 7. Create `NEW/sql/migrations/ArizonaMigration.sql`: Transform existing production data from Application→Site hierarchy into Group model. Map application_id+site_id to group_id, populate accounts/users/roles/groups/devices/sensors/device_groups tables, update sensor_readings and gateway_readings with group_id values.

