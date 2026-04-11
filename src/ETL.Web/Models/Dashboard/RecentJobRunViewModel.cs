namespace ETL.Web.Models.Dashboard;

public sealed class RecentJobRunViewModel
{
    public Guid JobId { get; init; }
    public string JobName { get; init; } = string.Empty;
    public DateTimeOffset StartedAtUtc { get; init; }
    public DateTimeOffset? CompletedAtUtc { get; init; }
    public string Status { get; init; } = string.Empty;
    public int RecordsLoaded { get; init; }
}
