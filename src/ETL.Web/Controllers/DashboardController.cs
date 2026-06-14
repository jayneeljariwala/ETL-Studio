using ETL.Application.Interfaces.Repositories;
using ETL.Domain.Common;
using ETL.Domain.Entities;
using ETL.Infrastructure.Identity;
using ETL.Web.Models.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ETL.Web.Controllers;

[Authorize]
public sealed class DashboardController : Controller
{
    private readonly IEtlJobRepository _etlJobRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<AppIdentityUser> _userManager;

    public DashboardController(
        IEtlJobRepository etlJobRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        UserManager<AppIdentityUser> userManager)
    {
        _etlJobRepository = etlJobRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var ownerId = await GetCurrentOwnerIdAsync(cancellationToken);

        var totalJobs = await _etlJobRepository.GetTotalJobsCountByOwnerAsync(ownerId, cancellationToken);
        var activeJobs = await _etlJobRepository.GetActiveJobsCountByOwnerAsync(ownerId, cancellationToken);
        var successfulRuns = await _etlJobRepository.GetSuccessfulRunsCountByOwnerAsync(ownerId, cancellationToken);
        var failedRuns = await _etlJobRepository.GetFailedRunsCountByOwnerAsync(ownerId, cancellationToken);

        var recentJobRuns = await _etlJobRepository.GetRecentJobRunsByOwnerAsync(10, ownerId, cancellationToken);
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

    private async Task<Guid> GetCurrentOwnerIdAsync(CancellationToken cancellationToken)
    {
        var identityUser = await _userManager.GetUserAsync(User);
        if (identityUser is null)
        {
            throw new DomainException("You must be logged in to view the dashboard.");
        }

        var appUser = await _userRepository.GetUserByIdentityIdAsync(identityUser.Id, cancellationToken);

        if (appUser is not null)
        {
            return appUser.Id;
        }

        appUser = ApplicationUser.Create(
            identityUser.Id,
            identityUser.UserName ?? identityUser.Email ?? "etl-user",
            identityUser.Email ?? $"{identityUser.Id}@local");

        _userRepository.AddUser(appUser);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return appUser.Id;
    }
}
