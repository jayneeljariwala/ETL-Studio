# Phase 4 - Services (ETL Engine)

## Application Layer Contracts

- `IEtlEngine` orchestrates extract -> transform -> load execution.
- `IDataExtractor` supports pluggable sources by `DataSourceType`.
- `IDataTransformer` applies field mappings and transformation pipeline.
- `IDataLoader` supports pluggable destinations by `DataDestinationType`.
- `ICustomExpressionEvaluator` evaluates custom C# expressions safely in an isolated script context.

## Request/Result Models

- `EtlExecutionRequest`
- `EtlExecutionResult`
- `FieldMappingDefinition`
- `TransformationDefinition`
- `RecordTransformResult`

## Infrastructure Implementations

- Extractors:
  - `CsvDataExtractor`
  - `ExcelDataExtractor`
  - `SqlServerDataExtractor`
  - `RestApiDataExtractor`
- Transform:
  - `TransformationPipeline`
  - `RoslynExpressionEvaluator`
- Loaders:
  - `PostgresDataLoader`
  - `SqlServerDataLoader`
- Engine:
  - `EtlEngine`

## Behavior

- Uses async streaming (`IAsyncEnumerable`) for extraction.
- Applies ordered mapping transforms:
  - Trim
  - Uppercase
  - Lowercase
  - Date format
  - Custom C# expression
- Validates required mapped fields.
- Loads in batches and supports:
  - `BulkInsert`
  - `Upsert`
- Produces execution counters:
  - read
  - transformed
  - loaded
  - failed

## DI Registration

`AddInfrastructure(...)` now registers:

- all extractors
- all loaders
- transformation services
- `IEtlEngine`
