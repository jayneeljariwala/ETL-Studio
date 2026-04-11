# Phase 5 - Controllers

## Added MVC Controllers

- `DashboardController` (`[Authorize]`)
  - `Index` with ETL KPIs and recent runs
- `EtlJobsController` (`[Authorize]`)
  - `Index`
  - `Details`
  - `Create` (GET/POST)
  - `Edit` (GET/POST)
  - `Run` (POST)
  - `Activate` (POST)
  - `Deactivate` (POST)
  - `History`
  - `Logs`

## Added Web View Models

- Dashboard:
  - `DashboardViewModel`
  - `RecentJobRunViewModel`
- ETL Jobs:
  - `EtlJobListItemViewModel`
  - `EtlJobUpsertViewModel`
  - `FieldMappingInputViewModel`
  - `TransformationInputViewModel`
  - `EtlJobDetailsViewModel`
  - `EtlJobRunHistoryViewModel`
  - `EtlJobRunHistoryItemViewModel`

## Controller Behavior Highlights

- Uses async EF Core queries (`AsNoTracking` for reads).
- Creates/updates domain entities through domain methods (`Create`, `UpdateDefinition`, status transitions).
- Executes ETL via `IEtlEngine` and writes execution outcomes into `ETLJobHistory`.
- Ensures domain `Users` profile exists for authenticated Identity user ownership.
