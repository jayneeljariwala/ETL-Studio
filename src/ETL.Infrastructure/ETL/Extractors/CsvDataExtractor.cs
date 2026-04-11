using CsvHelper;
using CsvHelper.Configuration;
using ETL.Application.ETL.Abstractions;
using ETL.Domain.Common;
using ETL.Domain.Enums;
using ETL.Infrastructure.ETL.Configuration;
using System.Globalization;

namespace ETL.Infrastructure.ETL.Extractors;

public sealed class CsvDataExtractor : IDataExtractor
{
    public DataSourceType SourceType => DataSourceType.Csv;

    public async IAsyncEnumerable<IReadOnlyDictionary<string, object?>> ExtractAsync(
        string sourceConfigurationJson,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var config = ConfigurationParser.ParseRequired<CsvSourceConfiguration>(sourceConfigurationJson, "CSV source");
        if (string.IsNullOrWhiteSpace(config.FilePath) || !File.Exists(config.FilePath))
        {
            throw new DomainException("CSV file path is missing or file does not exist.");
        }

        var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = config.Delimiter,
            HasHeaderRecord = config.HasHeaderRecord
        };

        await using var stream = File.OpenRead(config.FilePath);
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, csvConfig);

        if (config.HasHeaderRecord)
        {
            await csv.ReadAsync();
            csv.ReadHeader();
        }

        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var record = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            foreach (var header in csv.HeaderRecord ?? Array.Empty<string>())
            {
                record[header] = csv.GetField(header);
            }

            yield return record;
        }
    }
}
