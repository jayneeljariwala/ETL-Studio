# ETL Platform

An enterprise-grade Extract, Transform, Load (ETL) platform built using **ASP.NET Core MVC**. This project provides a flexible web-based engine to define connections, extract data from various source types, apply robust transformation pipelines (including custom C# scripts), and load data dynamically into target destinations.

## Architecture

This solution strictly adheres to **Clean Architecture**, promoting maintainability and clear separation of concerns across four primary layers:

*   **`ETL.Domain`**: Contains the core business entities, enums, and domain logic. It sits at the center of the architecture and has no external dependencies.
*   **`ETL.Application`**: Defines the application use-cases, request/result DTOs, and interface contracts for the ETL Engine (`IEtlEngine`, `IDataExtractor`, `IDataTransformer`, `IDataLoader`).
*   **`ETL.Infrastructure`**: Contains implementation details for external integrations. It implements Entity Framework Core data access, handles connections to third-party databases, and executes file parsing.
*   **`ETL.Web`**: The ASP.NET Core MVC presentation layer. It provides the front-end user interface to define ETL jobs, orchestrates background processing, and manages end-user interactions.

## Key Features

### 1. Pluggable Data Extraction
Data extractors use asynchronous streaming (`IAsyncEnumerable`) yielding one record at a time to minimize memory allocations on large datasets. Available data sources:
*   CSV Files (via `CsvHelper`)
*   Excel Spreadsheets (via `ExcelDataReader`)
*   SQL Server Databases
*   REST API Endpoints

### 2. Transformation Pipeline
Create robust mappings and apply sequential transformations directly onto raw data streams. Supported operations include:
*   Native string manipulation (Trim, Uppercase, Lowercase)
*   Format manipulation (e.g., Date and Time mapping)
*   **Dynamic C# Scripts**: Evaluate custom user-defined C# expressions securely via the *Roslyn* expression evaluator for complex, conditional transformations.

### 3. Data Loading
Efficient, batch-based loading mechanisms targeting structured relational schemas:
*   **Supported Destinations**: PostgreSQL and SQL Server.
*   **Insert Strategies**: Utilize standard bulk loading (`BulkInsert`) for high-throughput, or `Upsert` to elegantly avoid duplicates and map record alterations. 

### 4. Background Task Orchestration & Monitoring
*   Fully integrated with **Hangfire** allowing massive jobs to execute safely in the background, removing the burden from standard web request timeouts.
*   Maintains extensive live execution counters tracking metrics such as: rows read, successfully transformed, successfully loaded, and failed.
*   Centralized, structured event logging utilizing **Serilog**.

### 5. Security & Identity
*   Incorporates **ASP.NET Core Identity** for authentication, user management, and secure boundaries around defined tenant configurations.

## Run With Docker

Prerequisites:
*   Docker Desktop (or Docker Engine + Compose plugin)

Start the full stack (web + PostgreSQL):

```bash
docker compose up --build
```

Then open:
*   App: `http://localhost:8080`
*   Hangfire Dashboard: `http://localhost:8080/hangfire`

Stop and remove containers:

```bash
docker compose down
```

If you also want to remove the PostgreSQL volume (full DB reset):

```bash
docker compose down -v
```

Notes:
*   EF Core migrations are applied automatically on app startup.
*   The app container uses `ConnectionStrings__DefaultConnection` pointing at the `postgres` service.
