# Phase 7 - Background Processing

## Implemented

- Configured Hangfire with PostgreSQL storage in:
  - `src/ETL.Web/Program.cs`
- Added Hangfire server process:
  - `AddHangfireServer()`
- Added secure Hangfire dashboard endpoint:
  - `/hangfire`
  - Authenticated users only via `HangfireDashboardAuthorizationFilter`
- Added background ETL job worker:
  - `src/ETL.Infrastructure/BackgroundJobs/EtlJobBackgroundJob.cs`

## Controller Flow Change

- `EtlJobsController.Run` now:
  1. Validates job/mappings
  2. Marks job as `Queued`
  3. Enqueues Hangfire job (`EtlJobBackgroundJob.ExecuteAsync`)
  4. Returns immediately to user with queue confirmation

## Background Execution Flow

- Worker loads ETL job from DB
- Validates active state and mapping availability
- Executes ETL through `IEtlEngine`
- Updates job status/history to:
  - `Succeeded` with counters
  - `Failed` with error details

## Package Added

- `Hangfire.PostgreSql` in `src/ETL.Web/ETL.Web.csproj`
