namespace ETL.Application.ETL.Models;

public sealed class FieldMappingDefinition
{
    public string SourceField { get; init; } = string.Empty;
    public string DestinationField { get; init; } = string.Empty;
    public bool IsRequired { get; init; }
    public string? DefaultValue { get; init; }
    public int Order { get; init; }
    public IReadOnlyCollection<TransformationDefinition> Transformations { get; init; } = Array.Empty<TransformationDefinition>();
}
