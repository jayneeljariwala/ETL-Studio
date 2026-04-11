using ETL.Domain.Enums;

namespace ETL.Application.ETL.Abstractions;

public interface IDataLoader
{
    DataDestinationType DestinationType { get; }

    Task<int> LoadAsync(
        string destinationConfigurationJson,
        LoadStrategy strategy,
        IReadOnlyCollection<IReadOnlyDictionary<string, object?>> records,
        CancellationToken cancellationToken);
}
