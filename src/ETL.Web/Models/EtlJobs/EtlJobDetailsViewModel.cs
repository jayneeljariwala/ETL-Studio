namespace ETL.Web.Models.EtlJobs;

public sealed class EtlJobDetailsViewModel
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string SourceType { get; init; } = string.Empty;
    public string DestinationType { get; init; } = string.Empty;
    public string LoadStrategy { get; init; } = string.Empty;
    public string CurrentStatus { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public string SourceConfigurationJson { get; init; } = "{}";
    public string DestinationConfigurationJson { get; init; } = "{}";
    public IReadOnlyCollection<FieldMappingInputViewModel> FieldMappings { get; init; } = Array.Empty<FieldMappingInputViewModel>();
}
