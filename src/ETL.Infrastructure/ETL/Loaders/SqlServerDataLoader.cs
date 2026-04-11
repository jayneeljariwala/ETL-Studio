using System.Text;
using ETL.Application.ETL.Abstractions;
using ETL.Domain.Common;
using ETL.Domain.Enums;
using ETL.Infrastructure.ETL.Common;
using ETL.Infrastructure.ETL.Configuration;
using Microsoft.Data.SqlClient;

namespace ETL.Infrastructure.ETL.Loaders;

public sealed class SqlServerDataLoader : IDataLoader
{
    public DataDestinationType DestinationType => DataDestinationType.SqlServer;

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

        await using var connection = new SqlConnection(config.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var loaded = strategy switch
        {
            LoadStrategy.BulkInsert => await BulkInsertAsync(connection, (SqlTransaction)transaction, config, records, cancellationToken),
            LoadStrategy.Upsert => await UpsertAsync(connection, (SqlTransaction)transaction, config, records, cancellationToken),
            _ => throw new DomainException($"Unsupported load strategy: {strategy}.")
        };

        await transaction.CommitAsync(cancellationToken);
        return loaded;
    }

    private static async Task<int> BulkInsertAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        DestinationConfiguration config,
        IReadOnlyCollection<IReadOnlyDictionary<string, object?>> records,
        CancellationToken cancellationToken)
    {
        var columns = records.First().Keys.ToArray();
        var table = SqlIdentifierHelper.QuoteSqlServerTable(config.TableName);
        var quotedColumns = string.Join(", ", columns.Select(SqlIdentifierHelper.QuoteSqlServerColumn));

        var command = connection.CreateCommand();
        command.Transaction = transaction;
        var sql = new StringBuilder($"INSERT INTO {table} ({quotedColumns}) VALUES ");

        var recordIndex = 0;
        foreach (var record in records)
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
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<int> UpsertAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        DestinationConfiguration config,
        IReadOnlyCollection<IReadOnlyDictionary<string, object?>> records,
        CancellationToken cancellationToken)
    {
        if (config.KeyColumns.Count == 0)
        {
            throw new DomainException("Upsert requires at least one key column.");
        }

        var columns = records.First().Keys.ToArray();
        var table = SqlIdentifierHelper.QuoteSqlServerTable(config.TableName);
        var nonKeyColumns = columns.Where(col => !config.KeyColumns.Contains(col, StringComparer.OrdinalIgnoreCase)).ToArray();
        var loaded = 0;

        foreach (var record in records)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;

            var whereClauses = new List<string>(config.KeyColumns.Count);
            var setClauses = new List<string>(nonKeyColumns.Length);
            var insertColumns = string.Join(", ", columns.Select(SqlIdentifierHelper.QuoteSqlServerColumn));
            var insertValues = new List<string>(columns.Length);
            var parameterIndex = 0;

            foreach (var keyColumn in config.KeyColumns)
            {
                var parameterName = $"@p{parameterIndex++}";
                whereClauses.Add($"{SqlIdentifierHelper.QuoteSqlServerColumn(keyColumn)} = {parameterName}");
                command.Parameters.AddWithValue(parameterName, record[keyColumn] ?? DBNull.Value);
            }

            foreach (var nonKeyColumn in nonKeyColumns)
            {
                var parameterName = $"@p{parameterIndex++}";
                setClauses.Add($"{SqlIdentifierHelper.QuoteSqlServerColumn(nonKeyColumn)} = {parameterName}");
                command.Parameters.AddWithValue(parameterName, record[nonKeyColumn] ?? DBNull.Value);
            }

            foreach (var column in columns)
            {
                var parameterName = $"@p{parameterIndex++}";
                insertValues.Add(parameterName);
                command.Parameters.AddWithValue(parameterName, record[column] ?? DBNull.Value);
            }

            command.CommandText = $@"
UPDATE {table} SET {string.Join(", ", setClauses)} WHERE {string.Join(" AND ", whereClauses)};
IF @@ROWCOUNT = 0
BEGIN
    INSERT INTO {table} ({insertColumns}) VALUES ({string.Join(", ", insertValues)});
END";

            loaded += await command.ExecuteNonQueryAsync(cancellationToken) > 0 ? 1 : 0;
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
