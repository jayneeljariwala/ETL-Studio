using System.Text.RegularExpressions;
using ETL.Domain.Common;

namespace ETL.Infrastructure.ETL.Common;

internal static partial class SqlIdentifierHelper
{
    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$")]
    private static partial Regex IdentifierRegex();

    public static string QuotePostgresTable(string tableName)
    {
        var (schema, table) = ParseSchemaAndTable(tableName);
        return $"\"{schema}\".\"{table}\"";
    }

    public static string QuoteSqlServerTable(string tableName)
    {
        var (schema, table) = ParseSchemaAndTable(tableName);
        return $"[{schema}].[{table}]";
    }

    public static string QuotePostgresColumn(string columnName) => $"\"{ValidateIdentifier(columnName, "column")}\"";
    public static string QuoteSqlServerColumn(string columnName) => $"[{ValidateIdentifier(columnName, "column")}]";

    private static (string Schema, string Table) ParseSchemaAndTable(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
        {
            throw new DomainException("Destination table name is required.");
        }

        var parts = tableName.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length is 1)
        {
            return ("public", ValidateIdentifier(parts[0], "table"));
        }

        if (parts.Length is 2)
        {
            return (ValidateIdentifier(parts[0], "schema"), ValidateIdentifier(parts[1], "table"));
        }

        throw new DomainException("Table name must be in 'table' or 'schema.table' format.");
    }

    private static string ValidateIdentifier(string identifier, string identifierType)
    {
        if (!IdentifierRegex().IsMatch(identifier))
        {
            throw new DomainException($"Invalid {identifierType} identifier '{identifier}'.");
        }

        return identifier;
    }
}
