# Phase 6 - Views (Razor)

## Shared UI Updates

- Updated layout navigation and auth actions in:
  - `Views/Shared/_Layout.cshtml`
- Added flash message rendering for `TempData` success/error alerts.
- Updated view imports in:
  - `Views/_ViewImports.cshtml`
- Default route updated to `Dashboard/Index` in `Program.cs`.

## Added Dashboard Views

- `Views/Dashboard/Index.cshtml`
  - KPI cards
  - recent job runs table
  - quick action to create a job

## Added ETL Job Views

- `Views/EtlJobs/Index.cshtml`
- `Views/EtlJobs/Details.cshtml`
- `Views/EtlJobs/Create.cshtml`
- `Views/EtlJobs/Edit.cshtml`
- `Views/EtlJobs/History.cshtml`
- `Views/EtlJobs/Logs.cshtml`
- `Views/EtlJobs/_JobForm.cshtml` (shared create/edit form)

## Mapping UX

- Added client script:
  - `wwwroot/js/etl-job-form.js`
- Supports:
  - add/remove mapping rows
  - drag-and-drop row reordering
  - automatic index/order normalization before submit

