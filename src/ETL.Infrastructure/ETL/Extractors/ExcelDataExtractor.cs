using ETL.Application.ETL.Abstractions;
using ETL.Domain.Common;
using ETL.Domain.Enums;
using ETL.Infrastructure.ETL.Configuration;
using ExcelDataReader;

namespace ETL.Infrastructure.ETL.Extractors;

public sealed class ExcelDataExtractor : IDataExtractor
{
    static ExcelDataExtractor()
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
    }

    public DataSourceType SourceType => DataSourceType.Excel;

    public async IAsyncEnumerable<IReadOnlyDictionary<string, object?>> ExtractAsync(
        string sourceConfigurationJson,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var config = ConfigurationParser.ParseRequired<ExcelSourceConfiguration>(sourceConfigurationJson, "Excel source");
        if (string.IsNullOrWhiteSpace(config.FilePath) || !File.Exists(config.FilePath))
        {
            throw new DomainException("Excel file path is missing or file does not exist.");
        }

        using var stream = File.OpenRead(config.FilePath);
        using var reader = ExcelReaderFactory.CreateReader(stream);

        do
        {
            if (!string.IsNullOrWhiteSpace(config.SheetName) &&
                !string.Equals(reader.Name, config.SheetName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var headers = new List<string>();
            var headerRowLoaded = false;

            while (reader.Read())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!headerRowLoaded)
                {
                    if (config.UseHeaderRow)
                    {
                        headers = Enumerable.Range(0, reader.FieldCount)
                            .Select(index => reader.GetValue(index)?.ToString() ?? $"Column{index + 1}")
                            .ToList();
                        headerRowLoaded = true;
                        continue;
                    }

                    headers = Enumerable.Range(0, reader.FieldCount)
                        .Select(index => $"Column{index + 1}")
                        .ToList();
                    headerRowLoaded = true;
                }

                var record = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    record[headers[i]] = reader.GetValue(i);
                }

                yield return record;
                await Task.Yield();
            }

            if (!string.IsNullOrWhiteSpace(config.SheetName))
            {
                yield break;
            }
        } while (reader.NextResult());
    }
}
