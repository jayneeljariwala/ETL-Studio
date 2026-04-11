using System.Text.Json.Serialization;

namespace ETL.Infrastructure.ETL.Configuration;

public sealed class CsvSourceConfiguration
{
    public string FilePath { get; set; } = string.Empty;
    public string Delimiter { get; set; } = ",";
    public bool HasHeaderRecord { get; set; } = true;
}

public sealed class ExcelSourceConfiguration
{
    public string FilePath { get; set; } = string.Empty;
    public string? SheetName { get; set; }
    public bool UseHeaderRow { get; set; } = true;
}

public sealed class SqlSourceConfiguration
{
    public string ConnectionString { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
}

public sealed class RestApiSourceConfiguration
{
    public string Url { get; set; } = string.Empty;
    public string Method { get; set; } = "GET";
    public Dictionary<string, string> Headers { get; set; } = new();
    public string? RootArrayProperty { get; set; }
    public int TimeoutSeconds { get; set; } = 60;
}

public sealed class DestinationConfiguration
{
    public string ConnectionString { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public List<string> KeyColumns { get; set; } = new();
    public int BatchSize { get; set; } = 1000;
}
