# Phase 1 - Project Setup

## Solution Layout

- `src/ETL.Web` - ASP.NET Core MVC presentation layer
- `src/ETL.Application` - use cases, service contracts, DTOs
- `src/ETL.Domain` - domain entities and core business rules
- `src/ETL.Infrastructure` - EF Core, data access, readers/writers
- `tests/ETL.Application.Tests` - application layer unit tests

## Project References

- `ETL.Application` -> `ETL.Domain`
- `ETL.Infrastructure` -> `ETL.Application`, `ETL.Domain`
- `ETL.Web` -> `ETL.Application`, `ETL.Infrastructure`
- `ETL.Application.Tests` -> `ETL.Application`

## NuGet Baseline

### ETL.Web
- `Hangfire.AspNetCore`
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
- `Microsoft.AspNetCore.Identity.UI`
- `Microsoft.EntityFrameworkCore.Design`
- `Serilog.AspNetCore`
- `Serilog.Sinks.Console`
- `Serilog.Sinks.File`

### ETL.Infrastructure
- `CsvHelper`
- `ExcelDataReader`
- `ExcelDataReader.DataSet`
- `Microsoft.Data.SqlClient`
- `Microsoft.EntityFrameworkCore`
- `Microsoft.EntityFrameworkCore.Design`
- `Npgsql.EntityFrameworkCore.PostgreSQL`

### ETL.Application
- `Microsoft.CodeAnalysis.CSharp.Scripting`

### ETL.Application.Tests
- `Microsoft.NET.Test.Sdk`
- `xunit`
- `xunit.runner.visualstudio`
- `coverlet.collector`

## Shared Build Configuration

- `Directory.Build.props` enables:
  - nullable reference types
  - implicit usings
  - deterministic builds
  - latest C# language version
