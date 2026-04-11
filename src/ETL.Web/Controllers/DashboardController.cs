using ETL.Domain.Enums;
using ETL.Infrastructure.Persistence;
using ETL.Web.Models.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ETL.Web.Controllers;

[Authorize]
public sealed class DashboardController : Controller
{
    private readonly ApplicationDbContext _dbContext;

    public DashboardController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var totalJobs = await _dbContext.EtlJobs.CountAsync(cancellationToken);
        var activeJobs = await _dbContext.EtlJobs.CountAsync(x => x.IsActive, cancellationToken);
        var successfulRuns = await _dbContext.EtlJobHistory.CountAsync(x => x.Status == EtlJobStatus.Succeeded, cancellationToken);
        var failedRuns = await _dbContext.EtlJobHistory.CountAsync(x => x.Status == EtlJobStatus.Failed, cancellationToken);

        var recentRuns = await _dbContext.EtlJobHistory
            .AsNoTracking()
            .OrderByDescending(x => x.StartedAtUtc)
            .Take(10)
            .Join(_dbContext.EtlJobs.AsNoTracking(),
                history => history.EtlJobId,
                job => job.Id,
                (history, job) => new RecentJobRunViewModel
                {
                    JobId = job.Id,
                    JobName = job.Name,
                    StartedAtUtc = history.StartedAtUtc,
                    CompletedAtUtc = history.CompletedAtUtc,
                    Status = history.Status.ToString(),
                    RecordsLoaded = history.RecordsLoaded
                })
            .ToListAsync(cancellationToken);

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
