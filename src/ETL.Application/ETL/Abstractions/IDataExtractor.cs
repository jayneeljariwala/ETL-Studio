using ETL.Domain.Enums;

namespace ETL.Application.ETL.Abstractions;

public interface IDataExtractor
{
    DataSourceType SourceType { get; }

    IAsyncEnumerable<IReadOnlyDictionary<string, object?>> ExtractAsync(
        string sourceConfigurationJson,
        CancellationToken cancellationToken);
}
