# Phase 2 - Domain Models

## Core Entities

- `ApplicationUser`
  - Represents a business user linked to ASP.NET Identity via `IdentityId`
  - Stores profile basics used by ETL ownership
- `EtlJob`
  - Defines ETL source/destination configuration and load strategy
  - Maintains job state and execution history lifecycle methods
- `EtlJobHistory`
  - Tracks each ETL execution run with status, timestamps, counters, and error details
- `FieldMapping`
  - Captures source-to-destination column mapping per ETL job
  - Supports ordered transformation pipelines

## Supporting Types

- Enums:
  - `DataSourceType` (`Csv`, `Excel`, `SqlServer`, `RestApi`)
  - `DataDestinationType` (`SqlServer`, `PostgreSql`)
  - `EtlJobStatus` (`Draft`, `Queued`, `Running`, `Succeeded`, `Failed`)
  - `LoadStrategy` (`BulkInsert`, `Upsert`)
  - `TransformationType` (`Trim`, `Uppercase`, `Lowercase`, `DateFormat`, `CustomExpression`)
- Value object:
  - `TransformationStep` with validation for parameterized operations

## Domain Base Types

- `Entity` with `Id`
- `AuditableEntity` with `CreatedAtUtc` and `UpdatedAtUtc`
- `DomainException` for business-rule validation failures
