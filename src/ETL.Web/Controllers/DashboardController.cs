using ETL.Application.Interfaces.Repositories;
using ETL.Domain.Enums;
using ETL.Web.Models.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ETL.Web.Controllers;

[Authorize]
public sealed class DashboardController : Controller
{
    private readonly IEtlJobRepository _etlJobRepository;

    public DashboardController(IEtlJobRepository etlJobRepository)
    {
        _etlJobRepository = etlJobRepository;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var totalJobs = await _etlJobRepository.GetTotalJobsCountAsync(cancellationToken);
        var activeJobs = await _etlJobRepository.GetActiveJobsCountAsync(cancellationToken);
        var successfulRuns = await _etlJobRepository.GetSuccessfulRunsCountAsync(cancellationToken);
        var failedRuns = await _etlJobRepository.GetFailedRunsCountAsync(cancellationToken);

        var recentJobRuns = await _etlJobRepository.GetRecentJobRunsAsync(10, cancellationToken);
        var recentRuns = recentJobRuns.Select(history => new RecentJobRunViewModel
        {
            JobId = history.EtlJob!.Id,
            JobName = history.EtlJob.Name,
            StartedAtUtc = history.StartedAtUtc,
            CompletedAtUtc = history.CompletedAtUtc,
            Status = history.Status.ToString(),
            RecordsLoaded = history.RecordsLoaded
        }).ToList();

        return View(new DashboardViewModel
        {
            TotalJobs = totalJobs,
            ActiveJobs = activeJobs,
            SuccessfulRuns = successfulRuns,
            FailedRuns = failedRuns,
            RecentRuns = recentRuns
        });
    }
}
