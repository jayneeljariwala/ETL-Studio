# Phase 3 - DbContext and Migrations

## Implemented

- `ApplicationDbContext` based on `IdentityDbContext<AppIdentityUser>`
- Entity configurations using Fluent API:
  - `ApplicationUserConfiguration` -> `Users`
  - `EtlJobConfiguration` -> `ETLJobs`
  - `EtlJobHistoryConfiguration` -> `ETLJobHistory`
  - `FieldMappingConfiguration` -> `FieldMappings`
  - Owned table for transformation pipeline steps: `FieldMappingTransformationSteps`
- Infrastructure DI registration:
  - `AddInfrastructure(...)` for DbContext + PostgreSQL provider
- Identity user model:
  - `AppIdentityUser` for ASP.NET Identity
- Design-time factory for EF tooling:
  - `ApplicationDbContextFactory`

## Migrations

- Generated migration:
  - `Persistence/Migrations/20260404174943_InitialCreate.cs`
- Generated snapshot:
  - `Persistence/Migrations/ApplicationDbContextModelSnapshot.cs`

## Included Tables

- Identity tables (`AspNetUsers`, `AspNetRoles`, etc.)
- Domain tables:
  - `Users`
  - `ETLJobs`
  - `ETLJobHistory`
  - `FieldMappings`
  - `FieldMappingTransformationSteps`

## Migration Commands

```bash
dotnet ef database update \
  --project src/ETL.Infrastructure/ETL.Infrastructure.csproj \
  --startup-project src/ETL.Web/ETL.Web.csproj
```

```bash
dotnet ef migrations add <MigrationName> \
  --project src/ETL.Infrastructure/ETL.Infrastructure.csproj \
  --startup-project src/ETL.Web/ETL.Web.csproj \
  --output-dir Persistence/Migrations
```
