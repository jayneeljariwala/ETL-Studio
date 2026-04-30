using System.Text;
using ETL.Application.ETL.Abstractions;
using ETL.Domain.Common;
using ETL.Domain.Enums;
using ETL.Infrastructure.ETL.Common;
using ETL.Infrastructure.ETL.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace ETL.Infrastructure.ETL.Loaders;

public sealed class PostgresDataLoader : IDataLoader
{
    private readonly ILogger<PostgresDataLoader> _logger;

    public PostgresDataLoader(ILogger<PostgresDataLoader> logger)
    {
        _logger = logger;
    }

    public DataDestinationType DestinationType => DataDestinationType.PostgreSql;

    public async Task<int> LoadAsync(
        string destinationConfigurationJson,
        LoadStrategy strategy,
        IReadOnlyCollection<IReadOnlyDictionary<string, object?>> records,
        CancellationToken cancellationToken)
    {
        if (records.Count == 0)
        {
            return 0;
        }

        var config = ConfigurationParser.ParseRequired<DestinationConfiguration>(destinationConfigurationJson, "destination");
        ValidateDestinationConfig(config);

        await using var connection = new NpgsqlConnection(config.ConnectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            var builder = new NpgsqlConnectionStringBuilder(config.ConnectionString);
            var host = builder.Host ?? "unknown";
            var port = builder.Port;
            
            var extraMessage = "";
            if (host is "localhost" or "127.0.0.1")
            {
                extraMessage = " IMPORTANT: Since you are running in Docker, you likely need to use the service name 'postgres' instead of 'localhost'.";
            }
            else if (port == 5433)
            {
                extraMessage = " IMPORTANT: Port 5433 is typically used for host-machine access. From within Docker, you should likely use the internal port 5432.";
            }
            else if (ex is OperationCanceledException && !cancellationToken.IsCancellationRequested)
            {
                extraMessage = " The connection attempt timed out. Check if the database host is reachable and the port is correct.";
            }
            else if (ex is NpgsqlException nex && nex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
            {
                extraMessage = " Connection timed out. This often happens if the hostname is wrong or a firewall is blocking the connection.";
            }

            _logger.LogError(ex, "Failed to connect to PostgreSQL at {Host}:{Port}. Database: {Database}, User: {User}.{ExtraMessage}", 
                host, port, builder.Database, builder.Username, extraMessage);
            
            throw new DomainException($"Failed to connect to PostgreSQL at {host}:{port}. {extraMessage.Trim()}", ex);
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var loaded = strategy switch
        {
            LoadStrategy.BulkInsert => await BulkInsertAsync(connection, transaction, config, records, cancellationToken),
            LoadStrategy.Upsert => await UpsertAsync(connection, transaction, config, records, cancellationToken),
            _ => throw new DomainException($"Unsupported load strategy: {strategy}.")
        };

        await transaction.CommitAsync(cancellationToken);
        return loaded;
    }

    private static async Task<int> BulkInsertAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DestinationConfiguration config,
        IReadOnlyCollection<IReadOnlyDictionary<string, object?>> records,
        CancellationToken cancellationToken)
    {
        var columns = records.First().Keys.ToArray();
        var quotedColumns = string.Join(", ", columns.Select(SqlIdentifierHelper.QuotePostgresColumn));
        var table = SqlIdentifierHelper.QuotePostgresTable(config.TableName);

        var maxParameters = 65000;
        var maxRowsPerBatch = maxParameters / columns.Length;
        if (maxRowsPerBatch == 0) maxRowsPerBatch = 1;

        var loaded = 0;
        foreach (var chunk in records.Chunk(maxRowsPerBatch))
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            var sql = new StringBuilder($"INSERT INTO {table} ({quotedColumns}) VALUES ");

            var recordIndex = 0;
            foreach (var record in chunk)
        {
            if (recordIndex > 0)
            {
                sql.Append(", ");
            }

            sql.Append('(');
            for (var colIndex = 0; colIndex < columns.Length; colIndex++)
            {
                var parameterName = $"@p_{recordIndex}_{colIndex}";
                if (colIndex > 0)
                {
                    sql.Append(", ");
                }

                sql.Append(parameterName);
                command.Parameters.AddWithValue(parameterName, record[columns[colIndex]] ?? DBNull.Value);
            }

            sql.Append(')');
            recordIndex++;
        }

        command.CommandText = sql.ToString();
        loaded += await command.ExecuteNonQueryAsync(cancellationToken);
    }
    
    return loaded;
    }

    private static async Task<int> UpsertAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DestinationConfiguration config,
        IReadOnlyCollection<IReadOnlyDictionary<string, object?>> records,
        CancellationToken cancellationToken)
    {
        if (config.KeyColumns.Count == 0)
        {
            throw new DomainException("Upsert requires at least one key column.");
        }

        var columns = records.First().Keys.ToArray();
        var table = SqlIdentifierHelper.QuotePostgresTable(config.TableName);
        var quotedColumns = string.Join(", ", columns.Select(SqlIdentifierHelper.QuotePostgresColumn));
        var keyColumns = config.KeyColumns.Select(SqlIdentifierHelper.QuotePostgresColumn).ToArray();

        var updateColumns = columns
            .Where(col => !config.KeyColumns.Contains(col, StringComparer.OrdinalIgnoreCase))
            .Select(col =>
            {
                var quoted = SqlIdentifierHelper.QuotePostgresColumn(col);
                return $"{quoted} = EXCLUDED.{quoted}";
            })
            .ToArray();

        var loaded = 0;
        foreach (var record in records)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;

            var paramNames = new List<string>(columns.Length);
            for (var i = 0; i < columns.Length; i++)
            {
                var paramName = $"@p{i}";
                paramNames.Add(paramName);
                command.Parameters.AddWithValue(paramName, record[columns[i]] ?? DBNull.Value);
            }

            var valuesClause = string.Join(", ", paramNames);
            command.CommandText =
                $"INSERT INTO {table} ({quotedColumns}) VALUES ({valuesClause}) " +
                $"ON CONFLICT ({string.Join(", ", keyColumns)}) DO UPDATE SET {string.Join(", ", updateColumns)};";

            loaded += await command.ExecuteNonQueryAsync(cancellationToken);
        }

        return loaded;
    }

    private static void ValidateDestinationConfig(DestinationConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(config.ConnectionString))
        {
            throw new DomainException("Destination connection string is required.");
        }

        if (string.IsNullOrWhiteSpace(config.TableName))
        {
            throw new DomainException("Destination table name is required.");
        }
    }
}
