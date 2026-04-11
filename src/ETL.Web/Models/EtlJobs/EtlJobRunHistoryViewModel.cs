namespace ETL.Web.Models.EtlJobs;

public sealed class EtlJobRunHistoryViewModel
{
    public Guid JobId { get; init; }
    public string JobName { get; init; } = string.Empty;
    public IReadOnlyCollection<EtlJobRunHistoryItemViewModel> Runs { get; init; } = Array.Empty<EtlJobRunHistoryItemViewModel>();
}

public sealed class EtlJobRunHistoryItemViewModel
{
    public Guid HistoryId { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTimeOffset StartedAtUtc { get; init; }
    public DateTimeOffset? CompletedAtUtc { get; init; }
    public int RecordsRead { get; init; }
    public int RecordsTransformed { get; init; }
    public int RecordsLoaded { get; init; }
    public int RecordsFailed { get; init; }
    public string? ErrorMessage { get; init; }
}
