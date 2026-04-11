# Phase 8 - Logging and Error Handling

## Implemented

- Serilog configured as the primary logging pipeline in `Program.cs`
  - Console + rolling file sink (`logs/etl-.log`)
  - log context enrichment
  - category-level overrides for noisy sources
- Request logging enabled with:
  - method/path/status/elapsed time
  - correlation id enrichment
  - user and remote IP enrichment

## Correlation and Traceability

- Added `CorrelationIdMiddleware`
  - reads/writes `X-Correlation-ID`
  - sets `HttpContext.TraceIdentifier`
  - pushes `CorrelationId` into Serilog log context

## Global Exception Handling

- Added `GlobalExceptionHandlingMiddleware`
  - catches unhandled exceptions centrally
  - logs exception with method/path/correlation id
  - redirects browser requests to `/Home/Error`
  - returns JSON error payload for API/non-HTML requests

## ETL Step Logging

- `EtlEngine` now logs:
  - job start and completion
  - per-batch load progress
  - validation failures with record index
  - fatal execution errors

## Updated Files

- `src/ETL.Web/Program.cs`
- `src/ETL.Web/Infrastructure/Observability/CorrelationIdMiddleware.cs`
- `src/ETL.Web/Infrastructure/Observability/GlobalExceptionHandlingMiddleware.cs`
- `src/ETL.Web/Controllers/HomeController.cs`
- `src/ETL.Infrastructure/ETL/Engine/EtlEngine.cs`
- `src/ETL.Web/appsettings.json`
- `src/ETL.Web/appsettings.Development.json`
