using ETL.Domain.Enums;

namespace ETL.Application.ETL.Models;

public sealed class EtlExecutionRequest
{
    public Guid EtlJobId { get; init; }
    public DataSourceType SourceType { get; init; }
    public string SourceConfigurationJson { get; init; } = string.Empty;
    public DataDestinationType DestinationType { get; init; }
    public string DestinationConfigurationJson { get; init; } = string.Empty;
    public LoadStrategy LoadStrategy { get; init; }
    public int BatchSize { get; init; } = 1000;
    public IReadOnlyCollection<FieldMappingDefinition> Mappings { get; init; } = Array.Empty<FieldMappingDefinition>();
}
