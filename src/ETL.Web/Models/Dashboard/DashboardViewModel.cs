namespace ETL.Web.Models.Dashboard;

public sealed class DashboardViewModel
{
    public int TotalJobs { get; init; }
    public int ActiveJobs { get; init; }
    public int SuccessfulRuns { get; init; }
    public int FailedRuns { get; init; }
    public IReadOnlyCollection<RecentJobRunViewModel> RecentRuns { get; init; } = Array.Empty<RecentJobRunViewModel>();
}
