namespace ETL.Web.Models.EtlJobs;

public sealed class EtlJobListItemViewModel
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string SourceType { get; init; } = string.Empty;
    public string DestinationType { get; init; } = string.Empty;
    public string CurrentStatus { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
}
