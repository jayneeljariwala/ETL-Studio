using ETL.Application.ETL.Abstractions;
using ETL.Domain.Common;
using ETL.Domain.Enums;
using ETL.Infrastructure.ETL.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;

namespace ETL.Infrastructure.ETL.Extractors;

public sealed class SqlServerDataExtractor : IDataExtractor
{
    private readonly ILogger<SqlServerDataExtractor> _logger;

    public SqlServerDataExtractor(ILogger<SqlServerDataExtractor> logger)
    {
        _logger = logger;
    }

    public DataSourceType SourceType => DataSourceType.SqlServer;

    public async IAsyncEnumerable<IReadOnlyDictionary<string, object?>> ExtractAsync(
        string sourceConfigurationJson,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var config = ConfigurationParser.ParseRequired<SqlSourceConfiguration>(sourceConfigurationJson, "SQL source");
        if (string.IsNullOrWhiteSpace(config.ConnectionString) || string.IsNullOrWhiteSpace(config.Query))
        {
            throw new DomainException("SQL source requires a connection string and query.");
        }

        await using var connection = new SqlConnection(config.ConnectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            var builder = new SqlConnectionStringBuilder(config.ConnectionString);
            var dataSource = builder.DataSource ?? "unknown";
            
            var extraMessage = "";
            if (dataSource.Contains("localhost") || dataSource.Contains("127.0.0.1"))
            {
                extraMessage = " IMPORTANT: Since you are running in Docker, you likely need to use the service name instead of 'localhost'.";
            }

            _logger.LogError(ex, "Failed to connect to SQL Server Source at {DataSource}. Database: {Database}, User: {User}.{ExtraMessage}", 
                dataSource, builder.InitialCatalog, builder.UserID, extraMessage);
            
            throw new DomainException($"Failed to connect to SQL Server Source at {dataSource}. Check your connection string.{extraMessage}", ex);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = config.Query;
        command.CommandTimeout = 120;

        await using var reader = await command.ExecuteReaderAsync(System.Data.CommandBehavior.SequentialAccess, cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = await reader.IsDBNullAsync(i, cancellationToken) ? null : reader.GetValue(i);
            }

            yield return row;
        }
    }
}
